using eBRestarter.Classes.EBInterfaces;

namespace eBRestarter.Classes.EBFiles
{
    public sealed class Delete : IDelete
    {

        public void DeleteSingleFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception)
            {

            }
        }

        public void DeleteAllFilesInDirectory(string filePath)
        {
            string[] files = Directory.GetFiles(filePath);

            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        public void DeleteAllFileAndSubFolderInDirectory(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                foreach (string file in Directory.GetFiles(filePath))
                {
                    File.Delete(file);
                }

                foreach (string directory in Directory.GetDirectories(filePath))
                {
                    DeleteAllFileAndSubFolderInDirectory(directory);
                }

                Directory.Delete(filePath);

            }
        }
    }
}
