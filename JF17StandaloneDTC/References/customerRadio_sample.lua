------- Steps to create customized radio freqs
-- 1. create a customerRadio.lua file in Doc folder (file name case sensitive)
-- 2. follow below sample to change the channel freq as needed
-- 3. channel index from 22 to 200 (channels 1 to 21 are set by MissionEditor)

------- Begin: sample content of customerRadio.lua -------

presets[22] = 110750000
presets[40] = 210750000
presets[200] = 410750000

return presets

------- End: sample content of customerRadio.lua -------
