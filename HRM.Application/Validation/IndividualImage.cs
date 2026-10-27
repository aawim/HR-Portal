using System.Buffers.Binary;
namespace HRM.Services;

public static class IndividualImage
{
    public const int MaximumBytes=2*1024*1024;
    public static string? Validate(byte[]? bytes)
    {
        if(bytes==null)return null;
        if(bytes.Length==0||bytes.Length>MaximumBytes)throw new InvalidOperationException("Choose a PNG or JPEG image up to 2 MB.");
        if(bytes.Length>=33&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
        {
            var offset=8;var header=false;var data=false;var end=false;
            while(offset<=bytes.Length-12)
            {
                var length=BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset,4));
                if(length>(uint)(bytes.Length-offset-12))break;
                var kind=System.Text.Encoding.ASCII.GetString(bytes,offset+4,4);
                if(!header)
                {
                    if(kind!="IHDR"||length!=13)break;
                    Dimensions(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset+8,4)),BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset+12,4)));header=true;
                }
                if(kind=="IDAT")data=true;
                offset+=12+(int)length;
                if(kind=="IEND"){end=length==0&&offset==bytes.Length;break;}
            }
            if(header&&data&&end)return "image/png";
        }
        if(bytes.Length>4&&bytes[0]==255&&bytes[1]==216&&bytes[^2]==255&&bytes[^1]==217)
        {
            var offset=2;var dimensions=false;
            while(offset<bytes.Length-2)
            {
                if(bytes[offset++]!=255)break;
                while(offset<bytes.Length&&bytes[offset]==255)offset++;
                if(offset>=bytes.Length)break;var marker=bytes[offset++];
                if(marker==0xDA){if(dimensions)return "image/jpeg";break;}
                if(marker is 0xD8 or 0xD9 or >=0xD0 and <=0xD7)continue;
                if(offset+2>bytes.Length)break;var length=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset,2));
                if(length<2||offset+length>bytes.Length)break;
                if(marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
                {
                    if(length<8)break;Dimensions(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset+5,2)),BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset+3,2)));dimensions=true;
                }
                offset+=length;
            }
        }
        throw new InvalidOperationException("The selected file is not a supported PNG or JPEG image.");
    }
    private static void Dimensions(uint width,uint height)
    {if(width is 0 or >4096||height is 0 or >4096)throw new InvalidOperationException("Image dimensions must be between 1 and 4096 pixels.");}
    public static string? DataUrl(byte[]? bytes,string? contentType)
    {
        try{var actual=Validate(bytes);return actual!=null&&actual==contentType?$"data:{actual};base64,{Convert.ToBase64String(bytes!)}":null;}
        catch(InvalidOperationException){return null;}
    }
}
