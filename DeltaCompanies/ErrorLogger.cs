using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace DeltaCompanies
{
    public static class ErrorLogger
    {
        public static void LogError(Exception ex)
        {
            using (var _db = new DeltaCompaniesDBEntities())
            {
                var error = new ErrorLog();
                error.ErrorCode = ex.HResult;
                error.ErrorMsg = "Exception Message: " + ex.Message + "____Exception Stack Trace: " + ex.StackTrace + "____Inner Exception Message: " + ex.InnerException?.Message;
                error.CreatedDate = DateTime.UtcNow;

                string txtFile = "Error Code: " + error.ErrorCode + Environment.NewLine + error.ErrorMsg + Environment.NewLine + "Date Error Occured: " + error.CreatedDate;

                string path = Directory.GetParent(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName).FullName;

                File.WriteAllText(path + @"\Documents" + @"\" + "ErrorLog" + DateTime.UtcNow.ToShortDateString().Replace("/", "") + "_" + DateTime.UtcNow.ToLongTimeString().Replace(":", "").Replace(" ", "") + ".txt", txtFile);

                _db.ErrorLogs.Add(error);
                //_db.SaveChanges();
            }
        }
    }
}