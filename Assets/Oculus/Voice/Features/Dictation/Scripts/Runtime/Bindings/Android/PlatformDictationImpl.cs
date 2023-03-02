/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Dictation;
using Facebook.WitAi.Dictation.Events;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Utilities;
using Oculus.Voice.Core.Bindings.Android;
using Oculus.Voice.Dictation.Configuration;
using UnityEngine.Events;

namespace Oculus.Voice.Dictation.Bindings.Android
{
    public class PlatformDictationImpl : BaseAndroidConnectionImpl<PlatformDictationSDKBinding>, IDictationService, IServiceEvents
    {
        private readonly IDictationService _baseService;
        private bool _serviceAvailable = true;
        private WitDictationRuntimeConfiguration _dictationRuntimeConfiguration;
        public PlatformDictationImpl(IDictationService dictationService)
            : base("com.oculus.assistant.api.unity.dictation.UnityDictationServiceFragment")
        {
            _baseService = dictationService;
        }

        private DictationListenerBinding _listenerBinding;

        public bool PlatformSupportsDictation => service.IsSupported && _serviceAvailable;
        public bool Active => service.Active;
        public bool IsRequestActive => service.IsRequestActive;
        public bool MicActive => service.Active;
        public ITranscriptionProvider TranscriptionProvider { get; set; }

        public DictationEvents DictationEvents
        {
            get => _baseService.DictationEvents;
            set => _baseService.DictationEvents = value;
        }

        public Action OnServiceNotAvailableEvent;

        public override void Connect(string version)
        {
            base.Connect(version);
            _listenerBinding = new DictationListenerBinding(this, this);
            service.SetListener(_listenerBinding);
        }

        public override void Disconnect()
        {
            base.Disconnect();
        }

        public void SetDictationRuntimeConfiguration(WitDictationRuntimeConfiguration configuration)
        {
            _dictationRuntimeConfiguration = configuration;
        }

        public void Activate()
        {
            service.StartDictation(new DictationConfigurationBinding(_dictationRuntimeConfiguration));
        }

        public void Activate(WitRequestOptions requestOptions)
        {
            Activate();
        }

        public void ActivateImmediately()
        {
            Activate();
        }

        public void ActivateImmediately(WitRequestOptions requestOptions)
        {
            Activate();
        }

        public void Deactivate()
        {
            service.StopDictation();
        }

        public void OnServiceNotAvailable(string error, string message)
        {
            _serviceAvailable = false;
            OnServiceNotAvailableEvent?.Invoke();
        }
    }
}
