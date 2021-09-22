------- Steps to create customized radio freqs
-- 1. create a customerRadio.lua file in Doc folder (file name case sensitive)
-- 2. follow below sample to change the channel freq as needed
-- 3. channel index from 22 to 200 (channels 1 to 21 are set by MissionEditor)

------- Begin: sample content of customerRadio.lua -------

presets[22] = 110750000
presets[40] = 210750000
presets[200] = 410750000

--My customs. They reload on plane spawn. all freqs in the plane must end in 0 or 5
presets[23] = 1107500000 -- Results in 1107.500M
presets[24] = 11075000 -- Results in 11.075M
presets[25] = 5 -- Results in 0.000M
presets[26] = 907500000 -- Results in 007.500M 
presets[27] = 90750000 -- Results in 90.750M
presets[28] = 410750000 -- Results in 410.750
presets[29] = 510750000 -- Results in 510.750
presets[30] = 1107500000000 -- Results in -2147.484
presets[31] = 123456789 -- Results in 123.457M
presets[32] = 123456000 -- Results in 123.456M

return presets

------- End: sample content of customerRadio.lua -------
