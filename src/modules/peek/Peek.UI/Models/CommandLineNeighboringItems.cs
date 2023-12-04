// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Peek.Common.Models;

namespace Peek.UI.Models
{
    public class CommandLineNeighboringItems : IReadOnlyList<IFileSystemItem>
    {
        public IFileSystemItem this[int index] => Items[index] = Items[index];

        public int Count { get; }

        private IFileSystemItem[] Items { get; }

        public CommandLineNeighboringItems(IEnumerable<IFileSystemItem> items)
        {
            Items = items.ToArray();
            Count = Items.Length;
        }

        public IEnumerator<IFileSystemItem> GetEnumerator()
        {
            return new NeighboringItemsEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
