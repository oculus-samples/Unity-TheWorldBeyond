// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
//https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/

namespace TheWorldBeyond.Utils
{
    public class NamedArrayAttribute : PropertyAttribute
    {
        public readonly string[] Names;
        public NamedArrayAttribute(string[] names) => Names = names;
    }
}
