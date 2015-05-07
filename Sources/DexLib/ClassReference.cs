﻿using System;
using System.Diagnostics;
using System.Text;
using Dot42.DexLib.Metadata;

namespace Dot42.DexLib
{
    public class ClassReference : CompositeType, IMemberReference
    {
        protected string ns;
        protected string fullNameCache;
        protected string name;
        public const char NamespaceSeparator = '.';
        public const char InternalNamespaceSeparator = '/';

        public ClassReference()
        {
            TypeDescriptor = TypeDescriptors.FullyQualifiedName;
        }

        /// <summary>
        /// Fullname ctor
        /// </summary>
        public ClassReference(string fullname) : this()
        {
            Fullname = fullname;
        }

        /// <summary>
        /// Namespace + name ctor
        /// </summary>
        public ClassReference(string @namespace, string name)
            : this()
        {
            Namespace = @namespace;
            Name = name;
        }

        /// <summary>
        /// Is other equal to this?
        /// </summary>
        public bool Equals(IMemberReference other)
        {
            return Equals(other as ClassReference);
        }

        /// <summary>
        /// Is other equal to this?
        /// </summary>
        public bool Equals(ClassReference other)
        {
            return base.Equals(other) && Fullname == other.Fullname;
        }

        /// <summary>
        /// Is other equal to this?
        /// </summary>
        public override bool Equals(TypeReference other)
        {
            return Equals(other as ClassReference);
        }

        /// <summary>
        /// Create a hashcode.
        /// </summary>
        public override int GetHashCode()
        {
            return Fullname.GetHashCode();
        }

        /// <summary>
        /// Namespace
        /// </summary>
        public virtual string Namespace
        {
            get { return ns; }
            set
            {
                ns = value;
                fullNameCache = null;
            }
        }

        public virtual string Fullname
        {
            get
            {
                if (fullNameCache == null)
                {
                    var result = new StringBuilder(Namespace);
                    if (result.Length > 0)
                        result.Append(NamespaceSeparator);
                    result.Append(Name);
                    fullNameCache = result.ToString();
                }
                return fullNameCache;
            }
            set
            {
                value = value.Replace(InternalNamespaceSeparator, NamespaceSeparator);
                var items = value.Split(NamespaceSeparator);
                if (items.Length > 0)
                {
                    name = items[items.Length - 1];
                    Array.Resize(ref items, items.Length - 1);
                    ns = string.Join(NamespaceSeparator.ToString(), items);
                }
                else
                {
                    name = string.Empty;
                    ns = string.Empty;
                }
                fullNameCache = value;
            }
        }

        #region IMemberReference Members

        public virtual string Name
        {
            get { return name; }
            set
            {
                name = value;
                fullNameCache = null;
            }
        }

        #endregion

        public override string ToString()
        {
            return Fullname;
        }

        /// <summary>
        /// Get this type reference in descriptor format.
        /// </summary>
        public override string Descriptor
        {
            get { return "L" + Fullname.Replace(NamespaceSeparator, InternalNamespaceSeparator) + ";"; }
        }
    }
}