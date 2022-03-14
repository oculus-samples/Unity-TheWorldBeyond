/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using System;

namespace Oculus.Interaction.CameraTool
{
    public class ThumbnailStore : MonoBehaviour, IThumbnailProvider
    {
        public event Action<IThumbnail> WhenThumbnailProvided = delegate { };

        [SerializeField, Interface(typeof(IThumbnailProvider))]
        private List<MonoBehaviour> _sources;
        private List<IThumbnailProvider> Sources;

        public IReadOnlyList<IThumbnail> Thumbnails => _thumbnails;

        private List<IThumbnail> _thumbnails = new List<IThumbnail>();

        protected bool _started = false;

        private void HandleNewThumbnail(IThumbnail thumbnail)
        {
            _thumbnails.Add(thumbnail);
            WhenThumbnailProvided.Invoke(thumbnail);
        }

        protected virtual void Awake()
        {
            Sources = _sources.ConvertAll(mono => mono as IThumbnailProvider);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            foreach (IThumbnailProvider source in Sources)
            {
                Assert.IsNotNull(source);
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Sources.ForEach((src) => src.WhenThumbnailProvided += HandleNewThumbnail);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Sources.ForEach((src) => src.WhenThumbnailProvided -= HandleNewThumbnail);
            }
        }
    }
}
