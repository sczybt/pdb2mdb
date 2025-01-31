// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Cci.Pdb {
  internal class PdbScope {
    internal PdbConstant[] constants;
    internal PdbSlot[] slots;
    internal PdbScope[] scopes;
    internal string[] usedNamespaces;

    internal uint segment;
    internal uint address;
    //internal uint offset;
    internal uint length;

    internal PdbScope(uint address, uint offset, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces) {
      this.constants = constants;
      this.slots = slots;
      this.scopes = ArrayT<PdbScope>.Empty;
      this.usedNamespaces = usedNamespaces;
      this.address = address;
     // this.offset = offset;
      this.length = length;
    }

    internal PdbScope(uint address, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces)
      : this(address, 0, length, slots, constants, usedNamespaces)
    {
    }

    internal PdbScope(uint funcOffset, BlockSym32 block, BitAccess bits, out uint typind) {
      this.segment = block.seg;
      this.address = block.off - funcOffset;
   //   this.offset = block.off - funcOffset;
      this.length = block.len;
      typind = 0;

      int constantCount;
      int scopeCount;
      int slotCount;
      int namespaceCount;
      PdbFunction.CountScopesAndSlots(bits, block.end, out constantCount, out scopeCount, out slotCount, out namespaceCount);
      constants = ArrayT<PdbConstant>.Create(constantCount);
      scopes = ArrayT<PdbScope>.Create(scopeCount);
      slots = ArrayT<PdbSlot>.Create(slotCount);
      usedNamespaces = ArrayT<string>.Create(namespaceCount);
      int constant = 0;
      int scope = 0;
      int slot = 0;
      int usedNs = 0;

      while (bits.Position < block.end) {
        ushort siz;
        ushort rec;

        bits.ReadUInt16(out siz);
        int star = bits.Position;
        int stop = bits.Position + siz;
        bits.Position = star;
        bits.ReadUInt16(out rec);

        switch ((SYM)rec) {
          case SYM.S_BLOCK32: {
              BlockSym32 sub = new BlockSym32();

              bits.ReadUInt32(out sub.parent);
              bits.ReadUInt32(out sub.end);
              bits.ReadUInt32(out sub.len);
              bits.ReadUInt32(out sub.off);
              bits.ReadUInt16(out sub.seg);
              bits.SkipCString(out sub.name);

              bits.Position = stop;
              scopes[scope++] = new PdbScope(funcOffset, sub, bits, out typind);
              break;
            }

          case SYM.S_MANSLOT:
            slots[slot++] = new PdbSlot(bits);
            bits.Position = stop;
                        typind = slots[slot - 1].typeToken;
            break;

          case SYM.S_UNAMESPACE:
            bits.ReadCString(out usedNamespaces[usedNs++]);
            bits.Position = stop;
            break;
           
          case SYM.S_END:
            bits.Position = stop;
            break;

          case SYM.S_MANCONSTANT:
            constants[constant++] = new PdbConstant(bits);
            bits.Position = stop;
            break;

          default:
            //throw new PdbException("Unknown SYM in scope {0}", (SYM)rec);
            bits.Position = stop;
            break;
        }
      }

      if (bits.Position != block.end) {
        throw new Exception("Not at S_END");
      }

      ushort esiz;
      ushort erec;
      bits.ReadUInt16(out esiz);
      bits.ReadUInt16(out erec);

      if (erec != (ushort)SYM.S_END) {
        throw new Exception("Missing S_END");
      }
    }
  }
}
