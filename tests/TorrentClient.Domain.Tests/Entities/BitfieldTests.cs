using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Domain.Entities;

namespace TorrentClient.Domain.Tests.Entities
{
    public class BitfieldTests
    {
        [Fact]
        public void HasPiece_FirstBit_ShouldReturnTrue()
        {
           
            var data = new byte[] { 0x80 };
            var bitfield = new Bitfield(data, 8);

            
            Assert.True(bitfield.HasPiece(0));
            Assert.False(bitfield.HasPiece(1));
        }

        [Fact]
        public void HasPiece_MultipleBits_ShouldReturnCorrectly()
        {
           
            var data = new byte[] { 0xA5 };
            var bitfield = new Bitfield(data, 8);

            Assert.True(bitfield.HasPiece(0));  
            Assert.False(bitfield.HasPiece(1)); 
            Assert.True(bitfield.HasPiece(2));  
            Assert.False(bitfield.HasPiece(3)); 
            Assert.False(bitfield.HasPiece(4)); 
            Assert.True(bitfield.HasPiece(5));  
            Assert.False(bitfield.HasPiece(6)); 
            Assert.True(bitfield.HasPiece(7));  
        }

        [Fact]
        public void CountAvailablePieces_ShouldReturnCorrectCount()
        {
            var data = new byte[] { 0xFF, 0x00 };
            var bitfield = new Bitfield(data, 16);

            
            int count = bitfield.CountAvailablePieces();

            
            Assert.Equal(8, count); 
        }
    }
}
