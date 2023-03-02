/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using UnityEngine.UI;

namespace Facebook.WitAi.TTS.Samples
{
    public class TTSErrorText : MonoBehaviour
    {
        // Label
        [SerializeField] private Text _errorLabel;
        // Current error response
        private string _error = string.Empty;

        // Add listeners
        private void Update()
        {
            if (TTSService.Instance != null)
            {
                string serviceError = TTSService.Instance.IsValid();
                if (!string.Equals(serviceError, _error))
                {
                    _error = serviceError;
                    if (string.IsNullOrEmpty(_error))
                    {
                        _errorLabel.text = string.Empty;
                    }
                    else
                    {
                        _errorLabel.text = $"TTS Service Error: {_error}";
                    }
                }
            }
        }
    }
}
