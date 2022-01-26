using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;


namespace K181185_QS3
{
    //BY K181185
    //Sec -F
    public partial class FileWatcherService : ServiceBase
    {
        //global variable LastExecuted maintained to compare in query
        DateTime LastExecuted;

        // define directory to check
        public static string sourceDir = ConfigurationManager.AppSettings["DirPath"];
        public static string targetDir = ConfigurationManager.AppSettings["DirPath2"];
        //txt file to log entries to check service functionality
        public static string eventlogPath = ConfigurationManager.AppSettings["eventlogPath"];

        private DirectoryInfo d = new DirectoryInfo(sourceDir); //Assuming test_folder is your dir to be monitored
        int ScheduleInterval = Convert.ToInt32(ConfigurationManager.AppSettings["ThreadTime"]);
        public Thread worker = null; //to manage intervals of 1 minute (or more)

        public FileWatcherService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                //last executed updated to current time at starting
                LastExecuted = DateTime.Now;
   
                File.AppendAllText(eventlogPath, "Service started at :" + LastExecuted);

                ThreadStart start = new ThreadStart(Working); //run working function on start
                worker = new Thread(start);
                worker.Start();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Working()
        {
            //infinite loop in order to keep repeating working function after delay
            for (; ;)
            {
                //yesN = yes there are new files, yesU = yes there are updated files
                int yesN, yesU;

                //call functions for checking new and updated files
                //call thread sleep

                File.AppendAllText(eventlogPath, "\nchecking for new files");

                yesN = CheckForNewFiles(LastExecuted);

                File.AppendAllText(eventlogPath, "\nchecking for updated files");

                yesU = CheckForUpdatedFiles(LastExecuted);

                LastExecuted = DateTime.Now;

                //if there are no changes then increase scheduleInterval
                if (yesN == 0 && yesU == 0)
                {
                    ScheduleInterval += 2;
                    if (ScheduleInterval > 59)
                    {
                        Thread.Sleep(60 * 60 * 1000); //delay cannot exceed 1 hour gap
                    }
                    File.AppendAllText(eventlogPath, "\nsleeping for " + ScheduleInterval + " minutes");
                    Thread.Sleep(ScheduleInterval * 60 * 1000); //sleep for +2 minutes if no changes
                }
                else
                {
                    //else if there are changes let delay be for 1 minute
                    ScheduleInterval = 1;
                    File.AppendAllText(eventlogPath, "\nsleeping for " + ScheduleInterval + " minutes");

                    Thread.Sleep(ScheduleInterval * 60 * 1000); //sleep for 1 minute
                }
            }
        }

        protected int CheckForNewFiles(DateTime since)
        {
            FileSystemInfo[] infos = d.GetFileSystemInfos("*");
            //check for files that were created after last execution/last checked time
            string[] NewFiles = (from info in infos
                                 where info.CreationTime > since
                                 select info.Name).ToArray();

            //call func to copy all new files to dir2
            if(NewFiles.Length > 0)
            {
                File.AppendAllText(eventlogPath, "\nnew files exist");

                //meaning that there are new files created
                CopyToFolder(NewFiles);

                //write new file names to log & log txt file
                File.AppendAllText(eventlogPath, "\nNew files since " + since);

                foreach (string Files in NewFiles)
                {
                    //write new file names
                    File.AppendAllText(eventlogPath, "\nNew Files :" + Files);

                }

                //if any new files exist return 1
                return 1;
            }

            //else if there are no new files
            return 0;
        }

        protected int CheckForUpdatedFiles(DateTime since)
        {
            FileSystemInfo[] infos = d.GetFileSystemInfos("*");
            //check for any updated files since last executed time and ALSO disregard any newly created files
            string[] UpdatedFiles = (from info in infos
                                     where info.LastWriteTime > since && info.CreationTime < since
                                     select info.Name).ToArray();

            if(UpdatedFiles.Length > 0)
            {
                File.AppendAllText(eventlogPath, "\nupdated files exist");

                //call func to copy all updated files (if any) to dir2
                CopyToFolder(UpdatedFiles);

                File.AppendAllText(eventlogPath, "\nUpdated files since " + since);

                foreach (string Files in UpdatedFiles)
                {
                    File.AppendAllText(eventlogPath, "\nUpdated Files :" + Files);
                }

                //if any updated files exist return 1
                return 1;

            }
            //else if no files updated return 0
            return 0;
        }

        protected override void OnStop()
        {
            try
            {
                if ((worker != null) & worker.IsAlive)
                {
                    worker.Abort();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        protected void CopyToFolder(string[] files)
        {
            //copy any updated and new files to a new folder
            //it recieves string array of all updated and new file names (not paths)
            string sourcePath, targetPath;
            foreach (string filename in files)
            {
                File.AppendAllText(eventlogPath, "\ncopying to dest folder");


                sourcePath = Path.Combine(sourceDir, filename);
                targetPath = Path.Combine(targetDir, filename);
                System.IO.File.Copy(sourcePath, targetPath, true);
            }

        }

     
    }
}
