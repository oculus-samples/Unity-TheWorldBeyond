// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Oculus.Platform;

public class AppEntitlementCheck : MonoBehaviour
{

    void Awake()
    {
        try
        {
            Core.AsyncInitialize();
            Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
        }
        catch (UnityException e)
        {
            Debug.LogError("Platform failed to initialize due to exception.");
            Debug.LogException(e);
            // Immediately quit the application.
            UnityEngine.Application.Quit();
        }
    }


    // Called when the Oculus Platform completes the async entitlement check request and a result is available.
    void EntitlementCallback(Message msg)
    {
        if (msg.IsError && !UnityEngine.Application.isEditor) // User failed entitlement check
        {
            // Implements a default behavior for an entitlement check failure -- log the failure and exit the app.
            Debug.LogError("You are NOT entitled to use this app.");
            UnityEngine.Application.Quit();
        }
        else // User passed entitlement check
        {
            // Log the succeeded entitlement check for debugging.
            Debug.Log("You are entitled to use this app.");
        }
    }
}
