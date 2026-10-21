"""Validate the complete local Go 1.8.x save format before copying it."""
import struct
import zlib

KEY=struct.unpack('<4I',bytes.fromhex('01136c5b0733c841678db51e07d6f164'))
MASK=0xffffffff;DELTA=0x9e3779b9
def decrypt(data):
 if len(data)<8 or len(data)%4:raise ValueError('invalid encrypted block')
 v=list(struct.unpack('<%dI'%(len(data)//4),data));n=len(v);y=v[0];total=((6+52//n)*DELTA)&MASK
 while total:
  e=(total>>2)&3
  for p in range(n-1,0,-1):
   z=v[p-1];mx=(((z>>5)^(y<<2))+((y>>3)^(z<<4)))^((total^y)+(KEY[(p&3)^e]^z))
   y=v[p]=(v[p]-mx)&MASK
  z=v[n-1];mx=(((z>>5)^(y<<2))+((y>>3)^(z<<4)))^((total^y)+(KEY[e]^z))
  y=v[0]=(v[0]-mx)&MASK;total=(total-DELTA)&MASK
 return struct.pack('<%dI'%n,*v)

def decode(data):
 h=struct.unpack_from('<8I',data,12);n=(h[3]+3)&~3
 if h[0]!=0 or h[1]!=0 or h[4]!=0 or h[7]!=32:raise ValueError('unsupported save format')
 if not 8<=h[2]<=16*1024*1024:raise ValueError('invalid payload size')
 if 44+n>len(data):raise ValueError('truncated')
 packed=decrypt(data[44:44+n]);unpacker=zlib.decompressobj();xml=unpacker.decompress(packed[:h[3]],h[2]+1)
 if not unpacker.eof or unpacker.unconsumed_tail:raise ValueError('incomplete payload')
 if len(xml)!=h[2] or xml[:4]!=b'XOX2':raise ValueError('invalid payload')
 count=struct.unpack_from('<I',xml,4)[0];base=12+count*4
 if count>100000 or base>len(xml):raise ValueError('invalid string table')
 offsets=struct.unpack_from('<%dI'%(count+1),xml,8)
 if offsets[0]!=0 or offsets[-1]>len(xml)-base:raise ValueError('invalid string offsets')
 if any(a>=b or xml[base+b-1]!=0 for a,b in zip(offsets,offsets[1:])):raise ValueError('invalid string offsets')
 strings=[xml[base+offsets[i]:base+offsets[i+1]-1].decode('utf8') for i in range(count)]
 return xml,strings

def game_hash(data, seed=0x04c11db7):
 def mix(a,b,c):
  a=((a-b-c)&MASK)^(c>>13);b=((b-c-a)&MASK)^((a<<8)&MASK);c=((c-a-b)&MASK)^(b>>13)
  a=((a-b-c)&MASK)^(c>>12);b=((b-c-a)&MASK)^((a<<16)&MASK);c=((c-a-b)&MASK)^(b>>5)
  a=((a-b-c)&MASK)^(c>>3);b=((b-c-a)&MASK)^((a<<10)&MASK);c=((c-a-b)&MASK)^(b>>15)
  return a,b,c
 a=b=0x9e3779b9;c=seed;pos=0
 while len(data)-pos>=12:
  x,y,z=struct.unpack_from('<3I',data,pos);a,b,c=mix((a+x)&MASK,(b+y)&MASK,(c+z)&MASK);pos+=12
 c=(c+len(data))&MASK
 for i,v in enumerate(data[pos:]):
  if i<4:a=(a+(v<<(8*i)))&MASK
  elif i<8:b=(b+(v<<(8*(i-4))))&MASK
  else:c=(c+(v<<(8*(i-7))))&MASK
 return mix(a,b,c)[2] or 1

def validate(data):
 if len(data)<44:raise ValueError('short save')
 magic,length,checksum=struct.unpack_from('<3I',data)
 if magic!=0 or length<32 or len(data)!=((length+43)&~31):raise ValueError('invalid file length')
 if game_hash(data[12:12+length])!=checksum:raise ValueError('file checksum mismatch')
 xml,strings=decode(data)
 if game_hash(xml)!=struct.unpack_from('<I',data,36)[0]:raise ValueError('payload checksum mismatch')
 return xml,strings
