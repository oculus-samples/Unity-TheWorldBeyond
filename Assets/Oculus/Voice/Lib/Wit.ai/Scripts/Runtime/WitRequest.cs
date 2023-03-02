/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Lib;
using UnityEngine;
using Facebook.WitAi.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Facebook.WitAi
{
    /// <summary>
    /// Manages a single request lifecycle when sending/receiving data from Wit.ai.
    ///
    /// Note: This is not intended to be instantiated directly. Requests should be created with the
    /// WitRequestFactory
    /// </summary>
    public class WitRequest
    {
        /// <summary>
        /// Error code thrown when an exception is caught during processing or
        /// some other general error happens that is not an error from the server
        /// </summary>
        public const int ERROR_CODE_GENERAL = -1;

        /// <summary>
        /// Error code returned when no configuration is defined
        /// </summary>
        public const int ERROR_CODE_NO_CONFIGURATION = -2;

        /// <summary>
        /// Error code returned when the client token has not been set in the
        /// Wit configuration.
        /// </summary>
        public const int ERROR_CODE_NO_CLIENT_TOKEN = -3;

        /// <summary>
        /// No data was returned from the server.
        /// </summary>
        public const int ERROR_CODE_NO_DATA_FROM_SERVER = -4;

        /// <summary>
        /// Invalid data was returned from the server.
        /// </summary>
        public const int ERROR_CODE_INVALID_DATA_FROM_SERVER = -5;

        /// <summary>
        /// Request was aborted
        /// </summary>
        public const int ERROR_CODE_ABORTED = -6;

        /// <summary>
        /// Request to the server timeed out
        /// </summary>
        public const int ERROR_CODE_TIMEOUT = -7;

        public const string URI_SCHEME = "https";
        public const string URI_AUTHORITY = "api.wit.ai";
        public const int URI_DEFAULT_PORT = 0;

        public const string WIT_API_VERSION = "20220728";
        public const string WIT_SDK_VERSION = "0.0.46";

        public const string WIT_ENDPOINT_SPEECH = "speech";
        public const string WIT_ENDPOINT_MESSAGE = "message";
        public const string WIT_ENDPOINT_DICTATION = "dictation";
        public const string WIT_ENDPOINT_ENTITIES = "entities";
        public const string WIT_ENDPOINT_INTENTS = "intents";
        public const string WIT_ENDPOINT_TRAITS = "traits";
        public const string WIT_ENDPOINT_APPS = "apps";
        public const string WIT_ENDPOINT_UTTERANCES = "utterances";

        private WitConfiguration configuration;

        private string command;
        private string path;

        public QueryParam[] queryParams;

        private HttpWebRequest _request;
        private Stream _writeStream;

        private WitResponseNode responseData;

        private bool isActive;
        private bool responseStarted;

        public byte[] postData;
        public string postContentType;
        public string requestId = Guid.NewGuid().ToString();

        private object streamLock = new object();

        private int bytesWritten;
        private bool requestRequiresBody;

        /// <summary>
        /// Callback called when a response is received from the server off a partial transcription
        /// </summary>
        public event Action<WitRequest> onPartialResponse;

        /// <summary>
        /// Callback called when a response is received from the server
        /// </summary>
        public event Action<WitRequest> onResponse;

        /// <summary>
        /// Callback called when the server is ready to receive data from the WitRequest's input
        /// stream. See WitRequest.Write()
        /// </summary>
        public Action<WitRequest> onInputStreamReady;

        /// <summary>
        /// Returns the raw string response that was received before converting it to a JSON object.
        ///
        /// NOTE: This response comes back on a different thread. Do not attempt ot set UI control
        /// values or other interactions from this callback. This is intended to be used for demo
        /// and test UI, not for regular use.
        /// </summary>
        public Action<string> onRawResponse;

        /// <summary>
        /// Returns a partial utterance from an in process request
        ///
        /// NOTE: This response comes back on a different thread.
        /// </summary>
        public Action<string> onPartialTranscription;

        /// <summary>
        /// Returns a full utterance from a completed request
        ///
        /// NOTE: This response comes back on a different thread.
        /// </summary>
        public Action<string> onFullTranscription;

        public delegate void PreSendRequestDelegate(ref Uri src_uri, out Dictionary<string,string> headers);

        /// <summary>
        /// Allows customization of the request before it is sent out.
        ///
        /// Note: This is for devs who are routing requests to their servers
        /// before sending data to Wit.ai. This allows adding any additional
        /// headers, url modifications, or customization of the request.
        /// </summary>
        public static PreSendRequestDelegate onPreSendRequest;

        public delegate Uri OnCustomizeUriEvent(UriBuilder uriBuilder);
        /// <summary>
        /// Provides an opportunity to customize the url just before a request executed
        /// </summary>
        public OnCustomizeUriEvent onCustomizeUri;

        public delegate Dictionary<string, string> OnProvideCustomHeadersEvent();
        /// <summary>
        /// Provides an opportunity to provide custom headers for the request just before it is
        /// executed.
        /// </summary>
        public OnProvideCustomHeadersEvent onProvideCustomHeaders;

        /// <summary>
        /// Returns true if a request is pending. Will return false after data has been populated
        /// from the response.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// JSON data that was received as a response from the server after onResponse has been
        /// called
        /// </summary>
        public WitResponseNode ResponseData => responseData;

        /// <summary>
        /// Encoding settings for audio based requests
        /// </summary>
        public AudioEncoding audioEncoding = new AudioEncoding();

        private int statusCode;
        public int StatusCode => statusCode;

        private string statusDescription;
        private bool isRequestStreamActive;
        public bool IsRequestStreamActive => IsActive && isRequestStreamActive;

        public bool HasResponseStarted => responseStarted;

        private bool isServerAuthRequired;
        public string StatusDescription => statusDescription;

        public int Timeout => configuration ? configuration.timeoutMS : 10000;

        private bool configurationRequired;
        private string serverToken;
        private string callingStackTrace;
        private DateTime requestStartTime;
        private ConcurrentQueue<byte[]> writeBuffer = new ConcurrentQueue<byte[]>();

        public override string ToString()
        {
            return path;
        }

        public WitRequest(WitConfiguration configuration, string path,
            params QueryParam[] queryParams)
        {
            if (!configuration) throw new ArgumentException("Configuration is not set.");
            configurationRequired = true;
            this.configuration = configuration;
            this.command = path.Split('/').First();
            this.path = path;
            this.queryParams = queryParams;
        }

        public WitRequest(WitConfiguration configuration, string path, bool isServerAuthRequired,
            params QueryParam[] queryParams)
        {
            if (!isServerAuthRequired && !configuration)
                throw new ArgumentException("Configuration is not set.");
            configurationRequired = true;
            this.configuration = configuration;
            this.isServerAuthRequired = isServerAuthRequired;
            this.command = path.Split('/').First();
            this.path = path;
            this.queryParams = queryParams;
            if (isServerAuthRequired)
            {
                serverToken = WitAuthUtility.GetAppServerToken(configuration?.application?.id);
            }
        }

        public WitRequest(string serverToken, string path, params QueryParam[] queryParams)
        {
            configurationRequired = false;
            this.isServerAuthRequired = true;
            this.command = path.Split('/').First();
            this.path = path;
            this.queryParams = queryParams;
            this.serverToken = serverToken;
        }

        /// <summary>
        /// Key value pair that is sent as a query param in the Wit.ai uri
        /// </summary>
        public class QueryParam
        {
            public string key;
            public string value;
        }

        /// <summary>
        /// Start the async request for data from the Wit.ai servers
        /// </summary>
        public void Request()
        {
            responseStarted = false;

            UriBuilder uriBuilder = new UriBuilder();

            var endpointConfig = WitEndpointConfig.GetEndpointConfig(configuration);

            uriBuilder.Scheme = endpointConfig.UriScheme;

            uriBuilder.Host = endpointConfig.Authority;

            var api = endpointConfig.WitApiVersion;
            if (endpointConfig.Port > 0)
            {
                uriBuilder.Port = endpointConfig.Port;
            }

            uriBuilder.Query = $"v={api}";

            uriBuilder.Path = path;

            callingStackTrace = Environment.StackTrace;

            if (queryParams.Any())
            {
                var p = queryParams.Select(par =>
                    $"{par.key}={Uri.EscapeDataString(par.value)}");
                uriBuilder.Query += "&" + string.Join("&", p);
            }

            var uri = null == onCustomizeUri ? uriBuilder.Uri : onCustomizeUri(uriBuilder);
            StartRequest(uri);
        }

        private void StartRequest(Uri uri)
        {
            if (!configuration && configurationRequired)
            {
                statusDescription = "Configuration is not set. Cannot start request.";
                Debug.LogError(statusDescription);
                statusCode = ERROR_CODE_NO_CONFIGURATION;
                SafeInvoke(onResponse);
                return;
            }

            if (!isServerAuthRequired && string.IsNullOrEmpty(configuration.clientAccessToken))
            {
                statusDescription = "Client access token is not defined. Cannot start request.";
                Debug.LogError(statusDescription);
                statusCode = ERROR_CODE_NO_CLIENT_TOKEN;
                SafeInvoke(onResponse);
                return;
            }

            //allow app to intercept request and potentially modify uri or add custom headers
            //NOTE: the callback depends on knowing the original Uri, before it is modified
            Dictionary<string, string> customHeaders = null;
            if (onPreSendRequest != null)
            {
                onPreSendRequest(ref uri, out customHeaders);
            }

            // Create http web request
            _request = WebRequest.Create(uri.AbsoluteUri) as HttpWebRequest;
            if (Application.isBatchMode)
            {
                _request.KeepAlive = false;
            }

            if (isServerAuthRequired)
            {
                _request.Headers["Authorization"] =
                    $"Bearer {serverToken}";
            }
            else
            {
                _request.Headers["Authorization"] =
                    $"Bearer {configuration.clientAccessToken.Trim()}";
            }

            if (null != postContentType)
            {
                _request.Method = "POST";
                _request.ContentType = postContentType;
                _request.ContentLength = postData.Length;
            }

            // Configure additional headers
            if (WitEndpointConfig.GetEndpointConfig(configuration).Speech == command ||
                WitEndpointConfig.GetEndpointConfig(configuration).Dictation == command)
            {
                _request.ContentType = audioEncoding.ToString();
                _request.Method = "POST";
                _request.SendChunked = true;
            }

            _request.Headers["X-Wit-Client-Request-Id"] = requestId;

            requestRequiresBody = RequestRequiresBody(command);

            var configId = "not-yet-configured";
#if UNITY_EDITOR
            if (configuration)
            {
                if (string.IsNullOrEmpty(configuration.configId))
                {
                    configuration.configId = Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(configuration);
                }

                configId = configuration.configId;
            }
#endif

            _request.UserAgent = GetUserAgent(configuration);

            requestStartTime = DateTime.UtcNow;
            isActive = true;
            statusCode = 0;
            statusDescription = "Starting request";
            _request.Timeout = configuration ? configuration.timeoutMS : 10000;
            WatchMainThreadCallbacks();

            if (null != onProvideCustomHeaders)
            {
                foreach (var header in onProvideCustomHeaders())
                {
                    _request.Headers[header.Key] = header.Value;
                }
            }

            //apply any modified headers last, as this allows us to overwrite headers if need be
            if (customHeaders != null)
            {
                foreach (var pair in customHeaders)
                {
                    _request.Headers[pair.Key] = pair.Value;
                }
            }

            if (_request.Method == "POST")
            {
                var getRequestTask = _request.BeginGetRequestStream(HandleRequestStream, _request);
                ThreadPool.RegisterWaitForSingleObject(getRequestTask.AsyncWaitHandle,
                    HandleTimeoutTimer, _request, Timeout, true);
            }
            else
            {
                StartResponse();
            }
        }

        // Get config user agent
        private static string _operatingSystem;
        private static string _deviceModel;
        private static string _appIdentifier;
        private static string _unityVersion;
        public static event Func<string> OnProvideCustomUserAgent;
        public static string GetUserAgent(WitConfiguration configuration)
        {
            // Setup if needed
            if (_operatingSystem == null) _operatingSystem = UnityEngine.SystemInfo.operatingSystem;
            if (_deviceModel == null) _deviceModel = UnityEngine.SystemInfo.deviceModel;
            if (_appIdentifier == null) _appIdentifier = Application.identifier;
            if (_unityVersion == null) _unityVersion = Application.unityVersion;

            // Use config id if found
            string configId = configuration?.configId;

#if UNITY_EDITOR
            string userEditor = "Editor";
            if (configuration != null && string.IsNullOrEmpty(configuration.configId))
            {
                configuration.configId = Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(configuration);
                UnityEditor.AssetDatabase.SaveAssets();
                configId = configuration.configId;
            }
#else
            string userEditor = "Runtime";
#endif

            // If null, set not configured
            if (string.IsNullOrEmpty(configId))
            {
                configId = "not-yet-configured";
            }

            // Append custom user agents
            string customUserAgents = string.Empty;
            if (OnProvideCustomUserAgent != null)
            {
                foreach (Func<string> del in OnProvideCustomUserAgent.GetInvocationList())
                {
                    string custom = del();
                    if (!string.IsNullOrEmpty(custom))
                    {
                        customUserAgents += $",{custom}";
                    }
                }
            }

            // Return full string
            return $"voice-sdk-44.0.0.172.455,wit-unity-{WIT_SDK_VERSION},{_operatingSystem},{_deviceModel},{configId},{_appIdentifier},{userEditor},{_unityVersion}{customUserAgents}";
        }

        private bool RequestRequiresBody(string command)
        {
            return command == WitEndpointConfig.GetEndpointConfig(configuration).Speech ||
                   command == WitEndpointConfig.GetEndpointConfig(configuration).Dictation;
        }

        private void StartResponse()
        {
            var asyncResult = _request.BeginGetResponse(HandleResponse, _request);
            ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, HandleTimeoutTimer, _request, Timeout, true);
        }

        private void HandleTimeoutTimer(object state, bool timeout)
        {
            if (!timeout) return;

            // Clean up the current request if it is still going
            if (null != _request)
            {
                Debug.Log("Request timed out after " + (DateTime.UtcNow - requestStartTime));
                _request.Abort();
            }

            isActive = false;

            // Close any open stream resources and clean up streaming state flags
            CloseRequestStream();

            // Update the error state to indicate the request timed out
            statusCode = ERROR_CODE_TIMEOUT;
            statusDescription = "Request timed out.";

            SafeInvoke(onResponse);
        }

        private void HandleResponse(IAsyncResult asyncResult)
        {
            bool sentResponse = false;
            string stringResponse = "";
            responseStarted = true;
            try
            {
                WebResponse response = _request.EndGetResponse(asyncResult);

                try
                {
                    HttpWebResponse httpResponse = response as HttpWebResponse;
                    statusCode = (int) httpResponse.StatusCode;
                    statusDescription = httpResponse.StatusDescription;
                    using (var responseStream = httpResponse.GetResponseStream())
                    {
                        if (response.Headers["Transfer-Encoding"] == "chunked")
                        {
                            byte[] buffer = new byte[10240];
                            int bytes = 0;
                            int offset = 0;
                            int totalRead = 0;
                            while ((bytes = responseStream.Read(buffer, offset, buffer.Length - offset)) > 0)
                            {
                                totalRead += bytes;
                                stringResponse = Encoding.UTF8.GetString(buffer, 0, totalRead);
                                if (stringResponse.EndsWith("\r\n"))
                                {
                                    try
                                    {
                                        offset = 0;
                                        totalRead = 0;
                                        sentResponse |= ProcessStringResponse(stringResponse);
                                    }
                                    catch (JSONParseException e)
                                    {
                                        offset = bytes;
                                        Debug.LogWarning(
                                            "Received what appears to be a partial response or invalid json. Attempting to continue reading. Parsing error: " +
                                            e.Message + "\n" + stringResponse);
                                    }
                                }
                                else
                                {
                                    offset = totalRead;
                                }
                            }

                            // If the final transmission didn't end with \r\n process it as the final
                            // result
                            if (!stringResponse.EndsWith("\r\n") && !string.IsNullOrEmpty(stringResponse))
                            {
                                sentResponse |= ProcessStringResponse(stringResponse);
                            }

                            if (stringResponse.Length > 0 && null != responseData)
                            {
                                MainThreadCallback(() => onRawResponse?.Invoke(stringResponse));
                            }
                        }
                        else
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                stringResponse = reader.ReadToEnd();
                                MainThreadCallback(() => onRawResponse?.Invoke(stringResponse));
                                responseData = WitResponseJson.Parse(stringResponse);
                            }
                        }
                    }
                }
                catch (JSONParseException e)
                {
                    Debug.LogError("Server returned invalid data: " + e.Message + "\n" +
                                   stringResponse);
                    statusCode = ERROR_CODE_INVALID_DATA_FROM_SERVER;
                    statusDescription = "Server returned invalid data.";
                }
                catch (WebException e)
                {
                    // Ensure was not cancelled
                    if (e.Status != WebExceptionStatus.RequestCanceled)
                    {
                        Debug.LogError(
                            $"{e.Message}\nRequest Stack Trace:\n{callingStackTrace}\nResponse Stack Trace:\n{e.StackTrace}");
                        statusCode = (int) e.Status;
                        statusDescription = e.Message;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"{e.Message}\nRequest Stack Trace:\n{callingStackTrace}\nResponse Stack Trace:\n{e.StackTrace}");
                    statusCode = ERROR_CODE_GENERAL;
                    statusDescription = e.Message;
                }
                finally
                {
                    response.Close();
                }
            }
            catch (WebException e)
            {
                statusCode = (int) e.Status;
                if (e.Response is HttpWebResponse errorResponse)
                {
                    statusCode = (int) errorResponse.StatusCode;
                    try
                    {
                        using (var errorStream = errorResponse.GetResponseStream())
                        {
                            if (errorStream != null)
                            {
                                using (StreamReader errorReader = new StreamReader(errorStream))
                                {
                                    stringResponse = errorReader.ReadToEnd();
                                    MainThreadCallback(() => onRawResponse?.Invoke(stringResponse));
                                    responseData = WitResponseJson.Parse(stringResponse);
                                }
                            }
                        }
                    }
                    catch (JSONParseException)
                    {
                        // Response wasn't encoded error, ignore it.
                    }
                    catch (Exception errorResponseError)
                    {
                        // We've already caught that there is an error, we'll ignore any errors
                        // reading error response data and use the status/original error for validation
                        Debug.LogWarning(errorResponseError);
                    }
                }

                statusDescription = e.Message;
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    Debug.LogError(
                        $"Http Request Failed [{statusCode}]: {e.Message}\nRequest Stack Trace:\n{callingStackTrace}\nResponse Stack Trace:\n{e.StackTrace}");
                }
            }
            finally
            {
                isActive = false;
            }

            CloseRequestStream();

            if (null != responseData)
            {
                var error = responseData["error"];
                if (!string.IsNullOrEmpty(error))
                {
                    statusDescription = $"Error: {responseData["code"]}. {error}";
                    statusCode = statusCode == 200 ? ERROR_CODE_GENERAL : statusCode;
                }
            }
            else if (statusCode == 200)
            {
                statusCode = ERROR_CODE_NO_DATA_FROM_SERVER;
                statusDescription = "Server did not return a valid json response.";
                Debug.LogWarning(
                    "No valid data was received from the server even though the request was successful. Actual potential response data: \n" +
                    stringResponse);
            }

            // Send final response if have not yet
            if (!sentResponse)
            {
                // Final transcription
                string transcription = responseData.GetTranscription();
                if (!string.IsNullOrEmpty(transcription))
                {
                    MainThreadCallback(() => onFullTranscription?.Invoke(transcription));
                }
                // Final response
                SafeInvoke(onResponse);
            }

            // Complete
            responseStarted = false;
        }
        // Safely handles
        private bool ProcessStringResponse(string stringResponse)
        {
            // Decode full response
            responseData = WitResponseJson.Parse(stringResponse);

            // Handle responses
            bool hasResponse = responseData.HasResponse();
            bool final = hasResponse && responseData.GetIsFinal();

            // Return transcription
            string transcription = responseData.GetTranscription();
            if (!string.IsNullOrEmpty(transcription) && (!hasResponse || final))
            {
                // Call partial transcription
                if (!final)
                {
                    MainThreadCallback(() => onPartialTranscription?.Invoke(transcription));
                }
                // Call full transcription
                else
                {
                    MainThreadCallback(() => onFullTranscription?.Invoke(transcription));
                }
            }

            // No response
            if (!hasResponse)
            {
                return false;
            }

            // Call partial response
            SafeInvoke(onPartialResponse);

            // Call final response
            if (final)
            {
                SafeInvoke(onResponse);
            }

            // Return final
            return final;
        }
        private void HandleRequestStream(IAsyncResult ar)
        {
            try
            {
                StartResponse();
                var stream = _request.EndGetRequestStream(ar);
                bytesWritten = 0;

                if (null != postData)
                {
                    bytesWritten += postData.Length;
                    stream.Write(postData, 0, postData.Length);
                    CloseRequestStream();
                }
                else
                {
                    if (null == onInputStreamReady)
                    {
                        CloseRequestStream();
                    }
                    else
                    {
                        isRequestStreamActive = true;
                        SafeInvoke(onInputStreamReady);
                    }
                }

                _writeStream = stream;
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    statusCode = (int) e.Status;
                    statusDescription = e.Message;
                    SafeInvoke(onResponse);
                }
            }
        }

        private void SafeInvoke(Action<WitRequest> action)
        {
            if (action == null)
            {
                return;
            }
            MainThreadCallback(() =>
            {
                // We want to allow each invocation to run even if there is an exception thrown by one
                // of the callbacks in the invocation list. This protects shared invocations from
                // clients blocking things like UI updates from other parts of the sdk being invoked.
                foreach (Action<WitRequest> responseDelegate in action.GetInvocationList())
                {
                    try
                    {
                        responseDelegate.DynamicInvoke(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            });
        }

        public void AbortRequest()
        {
            CloseActiveStream();
            if (null != _request)
            {
                _request.Abort();
                _request = null;
            }
            if (statusCode == 0)
            {
                statusCode = ERROR_CODE_ABORTED;
                statusDescription = "Request was aborted";
            }
            isActive = false;
        }

        /// <summary>
        /// Method to close the input stream of data being sent during the lifecycle of this request
        ///
        /// If a post method was used, this will need to be called before the request will complete.
        /// </summary>
        public void CloseRequestStream()
        {
            if (requestRequiresBody && bytesWritten == 0)
            {
                AbortRequest();
            }
            else
            {
                CloseActiveStream();
            }
        }

        private void CloseActiveStream()
        {
            lock (streamLock)
            {
                isRequestStreamActive = false;
                if (null != _writeStream)
                {
                    try
                    {
                        _writeStream.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Write Stream - Close Failed\n{e}");
                    }
                    _writeStream = null;
                }
            }
        }

        /// <summary>
        /// Write request data to the Wit.ai post's body input stream
        ///
        /// Note: If the stream is not open (IsActive) this will throw an IOException.
        /// Data will be written synchronously. This should not be called from the main thread.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void Write(byte[] data, int offset, int length)
        {
            try
            {
                _writeStream.Write(data, offset, length);
                bytesWritten += length;
            }
            catch (ObjectDisposedException)
            {
                // Handling edge case where stream is closed remotely
                // This problem occurs when the Web server resets or closes the connection after
                // the client application sends the HTTP header.
                // https://support.microsoft.com/en-us/topic/fix-you-receive-a-system-objectdisposedexception-exception-when-you-try-to-access-a-stream-object-that-is-returned-by-the-endgetrequeststream-method-in-the-net-framework-2-0-bccefe57-0a61-517a-5d5f-2dce0cc63265
                Debug.LogWarning(
                    "Stream already disposed. It is likely the server reset the connection before streaming started.");
            }
            catch (IOException e)
            {
                Debug.LogWarning(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (requestRequiresBody && bytesWritten == 0)
            {
                Debug.LogWarning("Stream was closed with no data written. Aborting request.");
                AbortRequest();
            }
        }

        #region CALLBACKS
        // Check performing
        private CoroutineUtility.CoroutinePerformer _performer = null;
        // All actions
        private ConcurrentQueue<Action> _mainThreadCallbacks = new ConcurrentQueue<Action>();

        // Called from background thread
        private void MainThreadCallback(Action action)
        {
            _mainThreadCallbacks.Enqueue(action);
        }
        // While active, perform any sent callbacks
        private void WatchMainThreadCallbacks()
        {
            // Ifnore if already performing
            if (_performer != null)
            {
                return;
            }

            // Check callbacks every frame (editor or runtime)
            _performer = CoroutineUtility.StartCoroutine(PerformMainThreadCallbacks());
        }
        // Every frame check for callbacks & perform any found
        private System.Collections.IEnumerator PerformMainThreadCallbacks()
        {
            // While checking, continue
            while (HasMainThreadCallbacks())
            {
                // Wait for frame
                if (Application.isPlaying && !Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }
                // Wait for a tick
                else
                {
                    yield return null;
                }

                // Perform if possible
                while (_mainThreadCallbacks.Count > 0 && _mainThreadCallbacks.TryDequeue(out var result))
                {
                    result();
                }
            }

            // Done performing
            _performer = null;
        }
        // Check actions
        private bool HasMainThreadCallbacks()
        {
            return IsActive || isRequestStreamActive || HasResponseStarted || _mainThreadCallbacks.Count > 0;
        }
        #endregion
    }
}
