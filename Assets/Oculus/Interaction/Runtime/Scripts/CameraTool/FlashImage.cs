/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace Oculus.Interaction.CameraTool
{
    public class FlashImage : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _flashDuration = 0.3f;

        [SerializeField]
        private Image _image;

        private float _flashTimer;

        public void Flash()
        {
            _flashTimer = _flashDuration;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_image);
        }

        protected virtual void Update()
        {
            if (_flashTimer > 0 && _flashDuration > 0)
            {
                _image.enabled = true;
                float flashAmt = _flashTimer / _flashDuration;
                float flashAlpha = Mathf.Sin(flashAmt * Mathf.PI);

                Color color = _image.color;
                color.a = flashAlpha;
                _image.color = color;

                _flashTimer -= Time.deltaTime;
            }
            else
            {
                Color color = _image.color;
                color.a = 0f;

                _image.color = color;
                _image.enabled = false;
            }
        }
    }
}
