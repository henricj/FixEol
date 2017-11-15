FixEol
======

Convert a batch of files into UTF-8 with CR-LF line endings.

Use a slightly modified version of https://github.com/errepi/ude to detect the
encoding of the source files.

Be as careful as possible when updating files to avoid destroying data by using backup files
and moves within the same filesystem as the original file.

Use PLINQ and "async" to do as much as possible in parallel.
