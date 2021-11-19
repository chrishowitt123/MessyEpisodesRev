using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessyEpisodeRev3
{
    class Program
    {
        public static SqlDataReader SqlDataReader { get; private set; }
        public static string Output { get; private set; }

        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine("Getting Connection ...");

            var datasource = @"hsc-sql-2016\BITEAM";//Server
            var database = "TrakCareBI"; //Database

            //Connection string 
            string connString = @"Data Source=" + datasource + ";Initial Catalog="
                        + database + ";Persist Security Info=True;Trusted_Connection=True;";

            //Create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            conn.Open();

            string SQL = @"
        SELECT * 
FROM OPENQUERY(HSSDPRD, 
' SELECT TOP 1000
	APPT_PAPMI_DR->PAPMI_No as URN
    , APPT_PAPMI_DR->PAPMI_Deceased_Date as DeceasedDate
	, APPT_PAPMI_DR->PAPMI_Name as PatientSurname
	, APPT_PAPMI_DR->PAPMI_Name2 as PatientFirstName
	, APPT_PAPMI_DR->PAPMI_RowId->PAPER_Sex_DR->CTSEX_Desc as Gender
	, APPT_PAPMI_DR->PAPMI_RowId->PAPER_Dob as PaitentDOB
	, APPT_PAPMI_DR->PAPMI_PAPER_DR->PAPER_StName as AddressFirstLine
	, APPT_PAPMI_DR->PAPMI_PAPER_DR->PAPER_ForeignAddress as AddressSecondLine
	, APPT_PAPMI_DR->PAPMI_RowId->PAPER_Zip_DR->CTZIP_Code as PostCode
	, APPT_Adm_DR->PAADM_ADMNo as EpisodeNumber
    , APPT_Adm_DR->PAADM_AdmDocCodeDR->CTPCP_Desc as EpisodeCareProvider
    , APPT_Adm_DR->PAADM_DepCode_DR->CTLOC_Desc as EpisodeSpecialty
	, APPT_Adm_DR->PAADM_VisitStatus as EpisodeVisitStatus
    , APPT_Adm_DR->PAADM_RefStat_DR->RST_Desc as EpisodeReferralStatus
	, APPT_AS_ParRef->AS_Date as AppointmentDate
	, APPT_AS_ParRef->AS_SessStartTime as AppointmentTime
	, APPT_AS_ParRef->AS_RES_ParRef->RES_Desc As AppointmentCareProvider
    , APPT_AS_ParRef->AS_RES_ParRef->RES_CTLOC_DR->CTLOC_Desc as AppointmentLocationDescription
    , APPT_Adm_DR->PAADM_AdmDocCodeDR->CTPCP_CPGroup_DR->CPG_Desc as CareProviderGroup
    , APPT_Status as AppointmentReferralStatus
	, APPT_Outcome_DR->OUTC_Desc as AppointmentOutcome
FROM    RB_Appointment
--WHERE APPT_AS_ParRef->AS_Date >= ''2021-10-07'' 
WHERE APPT_PAPMI_DR->PAPMI_Name NOT LIKE ''zz%''
AND APPT_Adm_DR->PAADM_VisitStatus  = ''A'' 
AND APPT_Adm_DR->PAADM_Type = ''O''
--AND APPT_Outcome_DR->OUTC_Desc <> ''NULL''
--AND APPT_PAPMI_DR->PAPMI_No = 101208
AND APPT_Adm_DR->PAADM_ADMNo in (''O0000194058'') 
ORDER BY APPT_PAPMI_DR->PAPMI_No
')";

            SqlCommand cmd = new SqlCommand(SQL, conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 3600;

            // Create and fill DataTable with SQL query
            DataTable dt = new DataTable();
            dt.Columns.Add("URN", typeof(Int32));
            dt.Columns.Add("DeceasedDate", typeof(String));
            dt.Columns.Add("PatientSurname", typeof(String));
            dt.Columns.Add("PatientFirstName", typeof(String));
            dt.Columns.Add("Gender", typeof(String));
            dt.Columns.Add("PaitentDOB", typeof(String));
            dt.Columns.Add("AddressFirstLine", typeof(String));
            dt.Columns.Add("AddressSecondLine", typeof(String));
            dt.Columns.Add("PostCode", typeof(String));
            dt.Columns.Add("EpisodeNumber", typeof(String));
            dt.Columns.Add("EpisodeCareProvider", typeof(String));
            dt.Columns.Add("EpisodeSpecialty", typeof(String));
            dt.Columns.Add("EpisodeVisitStatus", typeof(String));
            dt.Columns.Add("EpisodeReferralStatus", typeof(String));
            dt.Columns.Add("AppointmentDate", typeof(String));
            dt.Columns.Add("AppointmentTime", typeof(String));
            dt.Columns.Add("AppointmentCareProvider", typeof(String));
            dt.Columns.Add("AppointmentLocationDescription", typeof(String));
            dt.Columns.Add("CareProviderGroup", typeof(String));
            dt.Columns.Add("AppointmentReferralStatus", typeof(String));
            dt.Columns.Add("AppointmentOutcome", typeof(String));

            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                adapter.Fill(dt);
            }
            conn.Close();

            //Console info
            watch.Stop();
            TimeSpan SqlTime = watch.Elapsed;
            Console.WriteLine($"SQL took {SqlTime.Minutes} minuites and {SqlTime.Seconds} seconds to return query");
            watch.Restart();
            Console.WriteLine("Working...");
            Console.WriteLine();

            //Create DataView from DataTable for sorting
            DataView dv = dt.DefaultView;

            //Sort DataView
            dv.Sort = "URN, EpisodeNumber, AppointmentDate desc, AppointmentTime desc";

            //Create sorted DataTable
            DataTable sortedDT = dv.ToTable();

            var appointmentGroup = sortedDT.AsEnumerable().GroupBy(r => new {  EpisodeNumber = r["EpisodeNumber"] });
            // Dictionary<List<string>, List<string>> appsDictonary = new Dictionary<List<string>, List<string>>();
            Dictionary<string, Dictionary<string, int>> appsDictonary = new Dictionary<string, Dictionary<string, int>>();
            List<string> appDescList = new List<string>();
            List<Tuple<string, int>> tupleList = new List<Tuple<string, int>>();


            var epn = "";

            foreach (var group in appointmentGroup)
            {
                epn = epn + group.Key.ToString().Replace("{ EpisodeNumber =", "").Replace("}", "").Trim();


                foreach (DataRow dr in group)
                {

                    var appDesc = dr["AppointmentLocationDescription"].ToString();
                    appDescList.Add(appDesc);

                }


                foreach (var a in appDescList)
                {
                    var count = appDescList.Where(i => i == a).Count();
                    Tuple<string, int> tuple = Tuple.Create(a, count);
                    tupleList.Add(tuple);

                    //HashSet<string> appDescSet = new HashSet<string>(appDescList);

                }

                tupleList = tupleList.Distinct().ToList();

                foreach(var t in tupleList)
                {

                    if (!appsDictonary.ContainsKey(epn))
                    {

                        appsDictonary.Add(epn, new Dictionary<string, int>(){{t.Item1, t.Item2}});

                    }

                    else
                    {

                        appsDictonary[epn].Add(t.Item1, t.Item2);

                    }

                }

                appDescList.Clear();
                tupleList.Clear();

            }

            var sb = new StringBuilder();

            using (var sw = new System.IO.StringWriter(sb))
            {
                new Newtonsoft.Json.JsonSerializer().Serialize(sw, appsDictonary);
            }

            Console.WriteLine(sb.ToString());

        }
    }
}
