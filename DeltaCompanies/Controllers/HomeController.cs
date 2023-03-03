using DeltaCompanies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DeltaCompanies.Controllers
{
    public class HomeController : Controller
    {
        DeltaCompaniesDBEntities dcDB = new DeltaCompaniesDBEntities();

        public ActionResult Index()
        {
            try
            {
                //List<JobOrder> lstJobOrders = dcDB.JobOrders.ToList();

                List<JobOrder> lstJobOrders = new List<JobOrder>() {
                    new JobOrder {
                        ID = 1,
                        Title = "FirstJob",
                        StatusID = 1,
                        Status = new Status() { ID = 1, Name = "Good" }
                    },
                    new JobOrder {
                        ID = 2,
                        Title = "SecondJob",
                        StatusID = 2,
                        Status = new Status() { ID = 1, Name = "Bad" }
                    },
                    new JobOrder {
                        ID = 3,
                        Title = "ThirdJob",
                        StatusID = 1,
                        Status = new Status() { ID = 1, Name = "Good" }
                    }
                };


                return View(lstJobOrders);
            }

            catch (Exception ex)
            {
                ErrorLogger.LogError(ex);

                return View();
            }
        }

        [HttpGet]
        public JsonResult GetStatusTypes()
        {
            //List<StatusDataModel> lstStatusDataModels = new List<StatusDataModel>();

            //foreach (Status status in dcDB.Status)
            //{
            //    lstStatusDataModels.Add(new StatusDataModel() { ID = status.ID, Name = status.Name });
            //}

            List<StatusDataModel> lstStatusDataModels = new List<StatusDataModel>() {
                new StatusDataModel {
                    ID = 1,
                    Name = "Good"
                },
                new StatusDataModel {
                    ID = 2,
                    Name = "Bad"
                }
            };

            return Json(lstStatusDataModels, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateStatus(JobOrder jobOrder)
        {
            //var record = dcDB.JobOrders.Where(x => x.ID == jobOrder.ID).First();
            //record.StatusID = jobOrder.StatusID;
            //dcDB.SaveChanges();

            return Json(jobOrder, JsonRequestBehavior.AllowGet);
        }
    }
}