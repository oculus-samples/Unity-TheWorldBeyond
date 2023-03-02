/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Events;
using Facebook.WitAi.Interfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Facebook.WitAi.Dictation.Events
{
    [Serializable]
    public class DictationEvents : ITranscriptionEvent, IAudioInputEvents
    {
        [Header("Transcription Events")]
        [FormerlySerializedAs("OnPartialTranscription")]
        [Tooltip("Message fired when a partial transcription has been received.")]
        public WitTranscriptionEvent onPartialTranscription = new WitTranscriptionEvent();

        [FormerlySerializedAs("OnFullTranscription")]
        [Tooltip("Message received when a complete transcription is received.")]
        public WitTranscriptionEvent onFullTranscription = new WitTranscriptionEvent();

        [Tooltip("Called when a response from Wit.ai has been received")]
        public WitResponseEvent onResponse = new WitResponseEvent();

        public UnityEvent onStart = new UnityEvent();

        public UnityEvent onStopped = new UnityEvent();

        public WitErrorEvent onError = new WitErrorEvent();

        public DictationSessionEvent onDictationSessionStarted = new DictationSessionEvent();

        public DictationSessionEvent onDictationSessionStopped = new DictationSessionEvent();

        public WitMicLevelChangedEvent onMicAudioLevel = new WitMicLevelChangedEvent();

        #region Shared Event API - Transcription

        public WitTranscriptionEvent OnPartialTranscription => onPartialTranscription;
        public WitTranscriptionEvent OnFullTranscription => onFullTranscription;

        #endregion

        #region Shared Event API - Microphone

        public WitMicLevelChangedEvent OnMicAudioLevelChanged => onMicAudioLevel;
        public UnityEvent OnMicStartedListening => onStart;
        public UnityEvent OnMicStoppedListening => onStopped;

        #endregion
    }
}
