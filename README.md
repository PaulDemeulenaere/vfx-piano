# vfx-piano

⚠️Hacky and convoluted workaround to adress the lack of CPU reading from GPU collision ⚠️

It used a camera to dectect particles only based on rendering data.
The computeShader "Reduction" extracts the needed information to minimize the transfer from GPU to CPU.

Inspired by this thread https://forum.unity.com/threads/getparticlesysteminfo-latency.1243120/#post-7919116

See also:
- https://youtu.be/Ahlv7WxlXxs
- https://youtu.be/UdYvtrFld0A
- https://youtu.be/69aD4iWMgrQ
