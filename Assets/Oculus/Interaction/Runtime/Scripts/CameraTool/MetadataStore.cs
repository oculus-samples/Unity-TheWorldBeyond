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
    /// <summary>
    /// Used to retrieve <see cref="IMetadata"/>
    /// associated with an image identifier.
    /// </summary>
    public class MetadataStore : MonoBehaviour, IMetadataProvider
    {
        public event Action<IMetadata> WhenMetadataProvided = delegate { };

        [SerializeField, Interface(typeof(IMetadataProvider))]
        private List<MonoBehaviour> _sources;
        private List<IMetadataProvider> Sources;

        protected bool _started = false;

        private Dictionary<string, IMetadata> _idToMetadata =
            new Dictionary<string, IMetadata>();

        public IMetadata Get(string imageId)
        {
            if (_idToMetadata.TryGetValue(imageId, out IMetadata result))
            {
                return result;
            }
            return null;
        }

        public void Remove(string imageId)
        {
            _idToMetadata.Remove(imageId);
        }

        private void HandleNewMetadata(IMetadata metadata)
        {
            Assert.IsFalse(_idToMetadata.ContainsKey(metadata.ImageID),
                "Metadata is being added to store with duplicate Image ID");
            _idToMetadata.Add(metadata.ImageID, metadata);
            WhenMetadataProvided.Invoke(metadata);
        }

        protected virtual void Awake()
        {
            Sources = _sources.ConvertAll(mono => mono as IMetadataProvider);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            foreach (IMetadataProvider source in Sources)
            {
                Assert.IsNotNull(source);
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Sources.ForEach((src) => src.WhenMetadataProvided += HandleNewMetadata);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Sources.ForEach((src) => src.WhenMetadataProvided -= HandleNewMetadata);
            }
        }
    }
}
