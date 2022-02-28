# vfx-piano

⚠️ Hacky and convoluted workaround to adress the lack of CPU reading from GPU collision ⚠️

It used a camera to dectect particles only based on rendering data.
The computeShader "Reduction" extracts the needed information to minimize the transfer from GPU to CPU.

Reference:
- Inspired by this thread https://forum.unity.com/threads/getparticlesysteminfo-latency.1243120/#post-7919116
- Taking inspiration from this article https://pixeleuphoria.com/blog/index.php/2021/01/23/synthesizing-procedural-audio/#htoc-creating-audio for synthesizing sound

Next Step:
- Avoid missing collision using a child system and GPU event to render collision data in an offscreen camera

See also:
- https://youtu.be/Ahlv7WxlXxs
- https://youtu.be/UdYvtrFld0A
- https://youtu.be/69aD4iWMgrQ
- https://forum.unity.com/threads/getparticlesysteminfo-latency.1243120/#post-7929589
