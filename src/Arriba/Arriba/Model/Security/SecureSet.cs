// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Arriba.Model.Security
{
    [Serializable]
    public class SecuredSet<T> : ICollection<KeyValuePair<SecurityIdentity, T>>
    {
        private readonly IDictionary<SecurityIdentity, T> _set;

        public SecuredSet()
        {
            _set = new Dictionary<SecurityIdentity, T>();
        }

        public IEnumerator<KeyValuePair<SecurityIdentity, T>> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_set).GetEnumerator();
        }

        public T this[SecurityIdentity index]
        {
            get
            {
                return _set[index];
            }
            set
            {
                _set[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return _set.Count;
            }
        }

        public bool IsReadOnly { get { return false; } }

        public void Remove(SecurityIdentity identity)
        {
            _set.Remove(identity);
        }

        public void Add(SecurityIdentity id, T value)
        {
            _set.Add(id, value);
        }

        public void Add(KeyValuePair<SecurityIdentity, T> item)
        {
            _set.Add(item);
        }

        void ICollection<KeyValuePair<SecurityIdentity, T>>.Clear()
        {
            _set.Clear();
        }

        bool ICollection<KeyValuePair<SecurityIdentity, T>>.Contains(KeyValuePair<SecurityIdentity, T> item)
        {
            return _set.Contains(item);
        }

        void ICollection<KeyValuePair<SecurityIdentity, T>>.CopyTo(KeyValuePair<SecurityIdentity, T>[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<SecurityIdentity, T>>.Remove(KeyValuePair<SecurityIdentity, T> item)
        {
            return _set.Remove(item);
        }
    }
}