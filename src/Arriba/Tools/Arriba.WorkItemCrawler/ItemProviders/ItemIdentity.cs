// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.ItemProviders
{
    public class ItemIdentity
    {
        public int ID { get; set; }

        public DateTimeOffset ChangedDate { get; set; }

        public ItemIdentity(int id, DateTimeOffset changedDate)
        {
            ID = id;
            ChangedDate = changedDate;
        }

        public override bool Equals(object o)
        {
            if (!(o is ItemIdentity)) return false;
            ItemIdentity other = (ItemIdentity)o;
            return ID.Equals(other.ID) && ChangedDate.Equals(other.ChangedDate);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode() ^ ChangedDate.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0:n0} | {1:s}", ID, ChangedDate);
        }
    }

    //public class ItemIdentityGeneric
    //{
    //    public object ID { get; set; }
    //    public IComparable ChangedDate { get; set; }
    //}
}
