using System;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;

namespace guideXOS.Misc {
    // Minimal loader for GXM (formerly MUE) single-image executables.
    // Layout: [0..3] 'G','X','M','\0' (or legacy 'M','U','E'+'\0')
    //         [4..7]  version (u32)
    //         [8..11] entry RVA (u32)
    //         [12..15] image size (u32)
    //         [16..]  raw image
    public static unsafe class GXMLoader {
        public static bool TryExecute(byte[] image, out string error) {
            error = null; if (image == null || image.Length < 16) { error = "Executable too small"; return false; }
            byte b0 = image[0], b1 = image[1], b2 = image[2], b3 = image[3];
            bool sigGXM = (b0=='G' && b1=='X' && b2=='M' && b3==0);
            bool sigMUE = (b0=='M' && b1=='U' && b2=='E' && b3==0);
            if (!sigGXM && !sigMUE) { error = "Bad signature"; return false; }
            uint ver = ReadU32(image, 4);
            uint entryRva = ReadU32(image, 8);
            uint size = ReadU32(image, 12);
            if (size > (uint)image.Length) size = (uint)image.Length; if (entryRva >= size || size < 16) { error = "Invalid header"; return false; }

            // New: Optional GUI script preface after header.
            // If the bytes at [16..19] equal 'G','U','I'+'\0', then from [20..] until a double-NUL sequence is a UTF-8 script.
            if (size >= 20 && image[16]=='G' && image[17]=='U' && image[18]=='I' && image[19]==0) {
                int pos = 20; int end = (int)size; var win = new GXMScriptWindow("Script", 420, 300);
                // parse lines separated by \n (0x0A), fields with '|'
                int lineStart = pos;
                while (pos < end) {
                    byte c = image[pos++];
                    if (c == 0 || c == (byte)('\n')) {
                        int len = pos - lineStart - 1; if (len > 0) { string line = ExtractUtf8(image, lineStart, len);
                            ApplyGuiLine(win, line);
                        }
                        if (c == 0) break; lineStart = pos;
                    }
                }
                // show window and return
                WindowManager.MoveToEnd(win); win.Visible = true; return true;
            }

            ulong allocSize = AlignUp(size, 4096);
            byte* basePtr = (byte*)Allocator.Allocate(allocSize); if (basePtr == null) { error = "OOM"; return false; }
            fixed (byte* src = image) Native.Movsb(basePtr, src, size);
            PageTable.MapUser((ulong)basePtr, (ulong)basePtr);
            for (ulong off = 4096; off < allocSize; off += 4096) PageTable.MapUser((ulong)basePtr + off, (ulong)basePtr + off);
            const ulong StackSize = 64 * 1024; byte* stack = (byte*)Allocator.Allocate(StackSize); if (stack == null) { error = "OOM stack"; return false; }
            PageTable.MapUser((ulong)stack, (ulong)stack);
            for (ulong off = 4096; off < StackSize; off += 4096) PageTable.MapUser((ulong)stack + off, (ulong)stack + off);
            ulong rsp = (ulong)stack + StackSize - 16; ulong rip = (ulong)basePtr + entryRva; SchedulerExtensions.EnterUserMode(rip, rsp); return true;
        }
        private static void ApplyGuiLine(GXMScriptWindow win, string line){ if(line==null||line.Length==0) return; int p0=IndexOf(line,'|',0); if(p0==-1) return; string cmd=line.Substring(0,p0); string rest=line.Substring(p0+1);
            if(StringEquals(cmd,"WINDOW")) { int p1=IndexOf(rest,'|',0); if(p1==-1) return; string title=rest.Substring(0,p1); string wh=rest.Substring(p1+1); int p2=IndexOf(wh,'|',0); if(p2==-1) return; int w=ToInt(wh.Substring(0,p2)); int h=ToInt(wh.Substring(p2+1)); win.Title=title; win.Width=w>160?w:160; win.Height=h>120?h:120; win.X=(Framebuffer.Width-win.Width)/2; win.Y=(Framebuffer.Height-win.Height)/2; }
            else if(StringEquals(cmd,"BUTTON")) { int i0=NextField(rest,0,out string f0); int id=ToInt(f0); int i1=NextField(rest,i0,out string f1); string text=f1; int i2=NextField(rest,i1,out string f2); int x=ToInt(f2); int i3=NextField(rest,i2,out string f3); int y=ToInt(f3); int i4=NextField(rest,i3,out string f4); int w=ToInt(f4); NextField(rest,i4,out string f5); int h=ToInt(f5); win.AddButton(id,text,x,y,w,h); }
            else if(StringEquals(cmd,"LABEL")) { int i0=NextField(rest,0,out string f0); string text=f0; int i1=NextField(rest,i0,out string f1); int x=ToInt(f1); NextField(rest,i1,out string f2); int y=ToInt(f2); win.AddLabel(text,x,y); }
            else if(StringEquals(cmd,"LIST")) { // LIST|Id|X|Y|W|H|items;
                int i0=NextField(rest,0,out string f0); int id=ToInt(f0);
                int i1=NextField(rest,i0,out string f1); int x=ToInt(f1);
                int i2=NextField(rest,i1,out string f2); int y=ToInt(f2);
                int i3=NextField(rest,i2,out string f3); int w=ToInt(f3);
                int i4=NextField(rest,i3,out string f4); int h=ToInt(f4);
                NextField(rest,i4,out string f5); win.AddList(id,x,y,w,h,f5);
            }
            else if(StringEquals(cmd,"DROPDOWN")) { // DROPDOWN|Id|X|Y|W|H|items;
                int i0=NextField(rest,0,out string f0); int id=ToInt(f0);
                int i1=NextField(rest,i0,out string f1); int x=ToInt(f1);
                int i2=NextField(rest,i1,out string f2); int y=ToInt(f2);
                int i3=NextField(rest,i2,out string f3); int w=ToInt(f3);
                int i4=NextField(rest,i3,out string f4); int h=ToInt(f4);
                NextField(rest,i4,out string f5); win.AddDropdown(id,x,y,w,h,f5);
            }
            else if(StringEquals(cmd,"ONCLICK")) { // ONCLICK|Id|Action|Arg
                int i0=NextField(rest,0,out string f0); int id=ToInt(f0);
                int i1=NextField(rest,i0,out string f1); string action=f1;
                NextField(rest,i1,out string f2); string arg=f2;
                win.AddOnClick(id, action, arg);
            }
            else if(StringEquals(cmd,"ONCHANGE")) { // ONCHANGE|Id|Action|Arg
                int i0=NextField(rest,0,out string f0); int id=ToInt(f0);
                int i1=NextField(rest,i0,out string f1); string action=f1;
                NextField(rest,i1,out string f2); string arg=f2;
                win.AddOnChange(id, action, arg);
            }
        }
        private static int NextField(string s,int start,out string field){ int i=IndexOf(s,'|',start); if(i==-1){ field=s.Substring(start); return s.Length; } field=s.Substring(start,i-start); return i+1; }
        private static int IndexOf(string s,char c,int start){ for(int i=start;i<s.Length;i++){ if(s[i]==c) return i; } return -1; }
        private static bool StringEquals(string a,string b){ if(a==null||b==null||a.Length!=b.Length) return false; for(int i=0;i<a.Length;i++){ char ca=a[i]; char cb=b[i]; if(ca>=65&&ca<=90) ca=(char)(ca+32); if(cb>=65&&cb<=90) cb=(char)(cb+32); if(ca!=cb) return false; } return true; }
        private static int ToInt(string s){ int n=0; bool neg=false; if(!string.IsNullOrEmpty(s)){ int i=0; if(s[0]=='-'){ neg=true; i=1; } for(;i<s.Length;i++){ char ch=s[i]; if(ch<'0'||ch>'9') break; n=n*10+(ch-'0'); } } return neg?-n:n; }
        private static string ExtractUtf8(byte[] b,int off,int len){ char[] ch=new char[len]; for(int i=0;i<len;i++) ch[i]=(char)b[off+i]; return new string(ch); }
        private static uint ReadU32(byte[] b, int off){ return (uint)(b[off] | (b[off+1]<<8) | (b[off+2]<<16) | (b[off+3]<<24)); }
        private static ulong AlignUp(uint v, uint a){ uint r = (v + a - 1) & ~(a - 1); return r; }
    }
}
