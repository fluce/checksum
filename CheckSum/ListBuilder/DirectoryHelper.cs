using System;
using System.Collections.Generic;
using System.IO;

namespace CheckSum.ListBuilder
{
    public static class DirectoryHelper
    {
        public static IEnumerable<string> EnumerateFiles(string folder)
        {
            string[] files;

            try
            {
                files = Directory.GetFiles(folder);
            }
            catch (UnauthorizedAccessException)
            {
                files = new string[] {};
            }

            foreach (string file in files)
            {
                yield return file;
            }

            try
            {
                files = Directory.GetDirectories(folder);
            }
            catch (UnauthorizedAccessException)
            {
                files = new string[] {};
            }

            foreach (string subDir in files)
            {
                foreach (var file in EnumerateFiles(subDir))
                {
                    yield return file;
                }
            }
        }

        public static IEnumerable<string> EnumerateDirectories(string folder)
        {
            string[] files;

            try
            {
                files = Directory.GetDirectories(folder);
            }
            catch (UnauthorizedAccessException)
            {
                files = new string[] { };
            }

            foreach (string subDir in files)
            {
                yield return subDir;
                foreach (var file in EnumerateDirectories(subDir))
                {
                    yield return file;
                }
            }
        }

    }
}