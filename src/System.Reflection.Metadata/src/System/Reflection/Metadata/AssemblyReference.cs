// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct AssemblyReference
    {
        private readonly MetadataReader _reader;

        // Workaround: JIT doesn't generate good code for nested structures, so use raw uint.
        private readonly uint _treatmentAndRowId;

        private static readonly Version s_version_1_1_0_0 = new Version(1, 1, 0, 0);
        private static readonly Version s_version_4_0_0_0 = new Version(4, 0, 0, 0);

        internal AssemblyReference(MetadataReader reader, uint treatmentAndRowId)
        {
            Debug.Assert(reader != null);
            Debug.Assert(treatmentAndRowId != 0);

            // only virtual bit can be set in highest byte:
            Debug.Assert((treatmentAndRowId & ~(TokenTypeIds.VirtualTokenMask | TokenTypeIds.RIDMask)) == 0);

            _reader = reader;
            _treatmentAndRowId = treatmentAndRowId;
        }

        private uint RowId
        {
            get { return _treatmentAndRowId & TokenTypeIds.RIDMask; }
        }

        private bool IsVirtual
        {
            get { return (_treatmentAndRowId & TokenTypeIds.VirtualTokenMask) != 0; }
        }

        public Version Version
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualVersion();
                }

                // change mscorlib version:
                if (RowId == _reader.WinMDMscorlibRef)
                {
                    return s_version_4_0_0_0;
                }

                return _reader.AssemblyRefTable.GetVersion(RowId);
            }
        }

        public AssemblyFlags Flags
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualFlags();
                }

                return _reader.AssemblyRefTable.GetFlags(RowId);
            }
        }

        public StringHandle Name
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualName();
                }

                return _reader.AssemblyRefTable.GetName(RowId);
            }
        }

        public StringHandle Culture
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualCulture();
                }

                return _reader.AssemblyRefTable.GetCulture(RowId);
            }
        }

        public BlobHandle PublicKeyOrToken
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualPublicKeyOrToken();
                }

                return _reader.AssemblyRefTable.GetPublicKeyOrToken(RowId);
            }
        }

        public BlobHandle HashValue
        {
            get
            {
                if (IsVirtual)
                {
                    return GetVirtualHashValue();
                }

                return _reader.AssemblyRefTable.GetHashValue(RowId);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            if (IsVirtual)
            {
                return GetVirtualCustomAttributes();
            }

            return new CustomAttributeHandleCollection(_reader, AssemblyReferenceHandle.FromRowId(RowId));
        }

        #region Virtual Rows
        private Version GetVirtualVersion()
        {
            switch ((AssemblyReferenceHandle.VirtualIndex)RowId)
            {
                case AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors:
                    return s_version_1_1_0_0;
                default:
                    return s_version_4_0_0_0;
            }
        }

        private AssemblyFlags GetVirtualFlags()
        {
            // use flags from mscorlib ref (specifically PublicKey flag):
            return _reader.AssemblyRefTable.GetFlags(_reader.WinMDMscorlibRef);
        }

        private StringHandle GetVirtualName()
        {
            return StringHandle.FromVirtualIndex(GetVirtualNameIndex((AssemblyReferenceHandle.VirtualIndex)RowId));
        }

        private StringHandle.VirtualIndex GetVirtualNameIndex(AssemblyReferenceHandle.VirtualIndex index)
        {
            switch (index)
            {
                case AssemblyReferenceHandle.VirtualIndex.System_ObjectModel:
                    return StringHandle.VirtualIndex.System_ObjectModel;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime:
                    return StringHandle.VirtualIndex.System_Runtime;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime:
                    return StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime:
                    return StringHandle.VirtualIndex.System_Runtime_WindowsRuntime;

                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml:
                    return StringHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml;

                case AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors:
                    return StringHandle.VirtualIndex.System_Numerics_Vectors;
            }

            Debug.Assert(false, "Unexpected virtual index value");
            return 0;
        }

        private StringHandle GetVirtualCulture()
        {
            return default(StringHandle);
        }

        private BlobHandle GetVirtualPublicKeyOrToken()
        {
            switch ((AssemblyReferenceHandle.VirtualIndex)RowId)
            {
                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime:
                case AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml:
                    // use key or token from mscorlib ref:
                    return _reader.AssemblyRefTable.GetPublicKeyOrToken(_reader.WinMDMscorlibRef);

                default:
                    // use contract assembly key or token:
                    var hasFullKey = (_reader.AssemblyRefTable.GetFlags(_reader.WinMDMscorlibRef) & AssemblyFlags.PublicKey) != 0;
                    return BlobHandle.FromVirtualIndex(hasFullKey ? BlobHandle.VirtualIndex.ContractPublicKey : BlobHandle.VirtualIndex.ContractPublicKeyToken, 0);
            }
        }

        private BlobHandle GetVirtualHashValue()
        {
            return default(BlobHandle);
        }

        private CustomAttributeHandleCollection GetVirtualCustomAttributes()
        {
            // return custom attributes applied on mscorlib ref
            return new CustomAttributeHandleCollection(_reader, AssemblyReferenceHandle.FromRowId(_reader.WinMDMscorlibRef));
        }
        #endregion
    }
}