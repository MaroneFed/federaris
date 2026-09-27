using System; using System.IO; using System.IO.Compression;
class P {
  static void Main(string[] a) {
    int size = a.Length > 1 ? 200 : 96; int cols = a.Length > 1 ? 5 : 10; var names = a.Length > 1 ? a[1].Split(',') : Fief.IconArt.Names; int rows = (names.Length + cols - 1) / cols;
    int W = cols * (size + 8), H = rows * (size + 8);
    byte[] img = new byte[W * H * 3];
    for (int i = 0; i < img.Length; i++) img[i] = 70;
    for (int n = 0; n < names.Length; n++) {
      float[] g = Fief.IconArt.Render(names[n], size, 0f);
      float[] o = Fief.IconArt.Render(names[n], size, 0.09f);
      int ox = (n % cols) * (size + 8) + 4, oy = (n / cols) * (size + 8) + 4;
      for (int j = 0; j < size; j++) for (int i = 0; i < size; i++) {
        int row = size - 1 - j; // texture row 0 = bottom
        int idx = ((oy + row) * W + ox + i) * 3;
        float og = o[j * size + i], gg = g[j * size + i];
        float bg = 70;
        float c = bg * (1 - og) + 20 * og; c = c * (1 - gg) + 250 * gg;
        img[idx] = (byte)c; img[idx+1] = (byte)(c * (gg > 0 ? 1 : 1)); img[idx+2] = (byte)c;
      }
    }
    WritePng(a.Length > 0 ? a[0] : "sheet.png", W, H, img);
  }
  static void WritePng(string path, int w, int h, byte[] rgb) {
    using var fs = File.Create(path);
    fs.Write(new byte[]{137,80,78,71,13,10,26,10});
    var ihdr = new MemoryStream(); var bw = new BinaryWriter(ihdr);
    WBE(bw, w); WBE(bw, h); bw.Write((byte)8); bw.Write((byte)2); bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0);
    Chunk(fs, "IHDR", ihdr.ToArray());
    var raw = new MemoryStream();
    for (int y = 0; y < h; y++) { raw.WriteByte(0); raw.Write(rgb, y * w * 3, w * 3); }
    var z = new MemoryStream(); using (var zs = new ZLibStream(z, CompressionLevel.Optimal, true)) { raw.Position = 0; raw.CopyTo(zs); }
    Chunk(fs, "IDAT", z.ToArray()); Chunk(fs, "IEND", new byte[0]);
  }
  static void WBE(BinaryWriter b, int v) { b.Write((byte)(v>>24)); b.Write((byte)(v>>16)); b.Write((byte)(v>>8)); b.Write((byte)v); }
  static void Chunk(Stream s, string type, byte[] data) {
    var b = new BinaryWriter(s); WBE(b, data.Length); var t = System.Text.Encoding.ASCII.GetBytes(type); b.Write(t); b.Write(data);
    uint crc = Crc(t, data); WBE(b, (int)crc);
  }
  static uint Crc(byte[] t, byte[] d) { uint c = 0xffffffff; foreach (var arr in new[]{t,d}) foreach (var x in arr) { c ^= x; for (int k=0;k<8;k++) c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1; } return c ^ 0xffffffff; }
}
