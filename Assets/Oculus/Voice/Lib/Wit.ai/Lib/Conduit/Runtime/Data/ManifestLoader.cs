/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Wit.LitJson;
using UnityEngine;

namespace Meta.Conduit
{
    /// <summary>
    /// Loads the manifest and resolves its actions so they can be used during dispatching.
    /// </summary>
    class ManifestLoader : IManifestLoader
    {
        /// <summary>
        /// Loads the manifest from file and into a <see cref="Manifest"/> structure.
        /// </summary>
        /// <param name="filePath">The path to the manifest file.</param>
        /// <returns>The loaded manifest object.</returns>
        public Manifest LoadManifest(string manifestLocalPath)
        {
            Debug.Log($"Loaded Conduit manifest from Resources/{manifestLocalPath}");
            int extIndex = manifestLocalPath.LastIndexOf('.');
            string ignoreEnd = extIndex == -1 ? manifestLocalPath : manifestLocalPath.Substring(0, extIndex);
            TextAsset jsonFile = Resources.Load<TextAsset>(ignoreEnd);
            if (jsonFile == null)
            {
                Debug.LogError($"Conduit Error - No Manifest found at Resources/{manifestLocalPath}");
                return null;
            }

            string rawJson = jsonFile.text;
            var manifest = JsonMapper.ToObject<Manifest>(rawJson);
            if (manifest.ResolveActions())
            {
                Debug.Log($"Successfully Loaded Conduit manifest");
            }
            else
            {
                Debug.LogError($"Fail to resolve actions from Conduit manifest");
            }

            return manifest;
        }
    }
}
