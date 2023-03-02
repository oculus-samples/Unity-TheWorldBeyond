/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using UnityEngine.UI;
using Facebook.WitAi.TTS.Utilities;

namespace Facebook.WitAi.TTS.Samples
{
    public class TTSSpeakerInput : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private InputField _input;
        [SerializeField] private TTSSpeaker _speaker;

        // Preset text fields
        private void Update()
        {
            if (!string.Equals(_title.text, _speaker.presetVoiceID))
            {
                _title.text = _speaker.presetVoiceID;
                _input.placeholder.GetComponent<Text>().text = $"Write something to say in {_speaker.presetVoiceID}'s voice";
            }
        }

        // Either say the current phrase or stop talking/loading
        public void SayPhrase()
        {
            if (_speaker.IsLoading || _speaker.IsSpeaking)
            {
                _speaker.Stop();
            }
            else
            {
                _speaker.Speak(_input.text);
            }
        }
    }
}
