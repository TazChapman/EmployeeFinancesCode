using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EmployeeFinances
{
    class Program
    {
        public static string FILES_LOCATION = "C:\\Projects\\EmployeeFinances\\";

        static void Main(string[] args)
        {
            Console.WriteLine("Load Employees DB");
            Employees emp = new Employees();
            Console.WriteLine("--EmployeeDB Loaded Successfully--");
            Console.WriteLine("----------------------------------");

            Console.WriteLine("Get Employee Number 7");
            Console.WriteLine(emp.GetByEmployeeId("7").ToString()+"\n");

            Console.WriteLine("Loading Finances Module");
            Finances fin = new Finances(emp.EmployeesDB);
            Console.WriteLine("--Finance Module Loaded Successfully--\n");

            Console.WriteLine("Calculating Paychecks");
            fin.CalculatePayChecks();
            Console.WriteLine("--Paychecks Calculated Successfully--\n");

            Console.WriteLine("Calculating Top 15% of Company by Gross Pay");
            fin.TopFifteenPercentOfCompany();
            Console.WriteLine("--Finished Calculation Successfully--\n");

            Console.WriteLine("Calculating Total State Taxes Paid by State");
            fin.GetAllStatesTaxesPaid();
            Console.WriteLine("--Finished Calculation Successfully--\n");

            Console.WriteLine("Results can be found in text files here: \n" + Program.FILES_LOCATION + "\n----Completed----");

            Console.ReadLine();
        }
    }

    public class Employees
    {
        public List<Employee> EmployeesDB { get; set; }

        public Employees()
        {
            EmployeesDB = new List<Employee>();
            loadEmployeesDB();
        }

        public void loadEmployeesDB()
        {
            String line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                using (StreamReader sr = new StreamReader(Program.FILES_LOCATION + "Employees.txt"))
                {

                    //Read the first line of text
                    line = sr.ReadLine();

                    //Continue to read until you reach end of file
                    while (line != null)
                    {
                        //write the lie to console window
                        //Console.WriteLine(line); //Print

                        //Save To DB
                        EmployeesDB.Add(new Employee(line));

                        //Read the next line
                        line = sr.ReadLine();
                    }

                    //close the file
                    sr.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }

        //Get Employee by Employee Id Method.
        public Employee GetByEmployeeId(string EmployeeNumber)
        {
            return EmployeesDB.Find(e => e.EmpNum.Equals(EmployeeNumber));
        }
    }
    
    public class Employee
    {
        //Employee Number, First Name, Last Name, hourly/salary/pay type, salary or hourly rate, DOB, State, 2 week amount of hours.
        //1,JIMMY,MOSLEY,H,29.77,9/5/05,NM,70
        public string EmpNum { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public char PayType { get; set; }
        public double PayRate { get; set; }
        public double GrossPay { get; set; }
        public DateTime StartDate { get; set; }
        public int YearsWorked { get; set; }
        public string State { get; set; }
        public int HoursWorked { get; set; }

        public Employee(string input)
        {
            string[] emp = input.Split(',');
            EmpNum = emp[0];
            First = emp[1];
            Last = emp[2];
            PayType = emp[3].ToCharArray()[0];
            PayRate = Convert.ToDouble(emp[4]);
            StartDate = Convert.ToDateTime(emp[5]);
            YearsWorked = DateTime.Today.Year - StartDate.Year;
            State = emp[6];
            HoursWorked = Convert.ToInt32(emp[7]);
            GrossPay = CalculateGrossPay();
        }

        public override string ToString()
        {
            return EmpNum + ", " + First + ", " + Last + ", " + PayType + ", " + PayRate.ToString("N2") +", " + StartDate.ToString("MM/dd/yyyy") + ", " + State + ", " + HoursWorked;
        }

        public double CalculateGrossPay()
        {
            int HOURS_IN_FULL_WEEK = 80;
            int HOURS_FOR_TIME_HALF = 10;
            //int TIME_AND_HALF = 150;
            double TIME_AND_HALF_TIMES = 1.5;
            //int TIME_AND_THREE_QUARTERS = 175;
            double TIME_AND_THREE_QUARTERS_TIMES = 1.75;
            //Assuming 52 week year with being paid every 2 weeks:
            int NUMBER_OF_SALARY_PAYCHECKS_PER_YEAR = 26;

            double grossPay = 0;

            if (PayType.Equals('S'))
            {
                //Salaried employees are paid just their annual.
                //But this is two week period so we need to divide by 26 
                grossPay = PayRate / NUMBER_OF_SALARY_PAYCHECKS_PER_YEAR;
            }
            else if (PayType.Equals('H'))
            {
                int hoursOver = HoursWorked;
                if (hoursOver > HOURS_IN_FULL_WEEK)
                {
                    hoursOver -= HOURS_IN_FULL_WEEK;
                    grossPay += HOURS_IN_FULL_WEEK * PayRate;

                    if (hoursOver > HOURS_FOR_TIME_HALF)
                    {
                        hoursOver -= HOURS_FOR_TIME_HALF;
                        grossPay += HOURS_FOR_TIME_HALF * (PayRate * TIME_AND_HALF_TIMES);

                        //hoursOver will still be over 0 at least 1;
                        grossPay += hoursOver * (PayRate * TIME_AND_THREE_QUARTERS_TIMES);
                    }
                    else
                    {
                        //Time over is 10 hours or less.
                        grossPay += hoursOver * (PayRate * TIME_AND_HALF_TIMES);
                    }
                }
                else
                {
                    //Hours is 80 or less.
                    grossPay = HoursWorked * PayRate;
                }
            }

            return grossPay;
        }
       
        public string ToFifteenPercentReportString()
        {
            //Output, First Name, Last Name, Number of Years Worked, Gross Pay.
            return First + ", " + Last + ", " + YearsWorked + ", " + GrossPay.ToString("N2");
        }
    }

    public class Finances
    {
        //Employee DB
        private List<Employee> EmployeesDB { get; set; }

        //Paycheck DB
        List<Paycheck> PaycheckDB = new List<Paycheck>();

        //State DB
        List<State> StatesDB = new List<State>();

        public Finances(List<Employee> employeesDB)
        {
            EmployeesDB = employeesDB;
        }

        //Have regular percentage and TIMES version of variables for Reporting with the first one and actual work with second one.
        public int FEDERAL_TAX_PERCENT_WITHHELD = 15;
        public double FEDERAL_TAX_PERCENT_WITHHELD_TIMES = .15;
        public Dictionary<string, double> STATE_TAX_PERCENTAGES = new Dictionary<string, double>()
        {
            {"UT",5},{"WY", 5},{"NV",5},
            {"CO",6.5},{"ID",6.5},{"AZ",6.5},{"OR",6.5},
            {"WA",7},{"NM", 7},{"TX",7}
        };
        public Dictionary<string, double> STATE_TAX_PERCENTAGES_TIMES = new Dictionary<string, double>()
        {
            {"UT",.05},{"WY", .05},{"NV",.05},
            {"CO",.065},{"ID",.065},{"AZ",.065},{"OR",.065},
            {"WA",.07},{"NM", .07},{"TX",.07}
        };


        public void CalculatePayChecks()
        {
            //Calculate all of the Paychecks:
            //Copy Database as to not disturb original order
            List<Employee> empDBCopy = new List<Employee>();
            empDBCopy.AddRange(EmployeesDB);

            foreach (Employee e in empDBCopy)
            {
                PaycheckDB.Add(new Paycheck(e));
            }

            //Order the paychecks by Gross.
            PaycheckDB = PaycheckDB.OrderByDescending(p => p.GrossPay).ToList();

            //Write To Text File
            using (StreamWriter sw = new StreamWriter(Program.FILES_LOCATION + "Paychecks.TXT"))
            {
                foreach (Paycheck p in PaycheckDB)
                {
                    sw.WriteLine(p.ToString());
                }
                //CloseFile
                sw.Close();
            }
        }
        
        public void TopFifteenPercentOfCompany()
        {
            //Get list of Top 15% of earners.
            //15% is the Count * .15 and we need to Sort Employees list then grab that number.
            double FIFTEEN_PERCENT_TIMES = .15;
            int EmployeesNeeded = Convert.ToInt32(EmployeesDB.Count * FIFTEEN_PERCENT_TIMES);
            
            //Copy Database as to not disturb original order
            List<Employee> empDBCopy = new List<Employee>();
            empDBCopy.AddRange(EmployeesDB);

            //Sort employees list, then grab number:
            empDBCopy = empDBCopy.OrderByDescending(e => e.GrossPay).ToList();
            empDBCopy = empDBCopy.Take(EmployeesNeeded).ToList();

            //Sort by Number of years worked High to Low. 2. Then alphabetically by last Name, 3. Alphabetical First Name.
            empDBCopy = empDBCopy.OrderByDescending(e => e.YearsWorked).ThenBy(e => e.Last).ThenBy(e => e.First).ToList();

            //Output, First Name, Last Name, Number of Years Worked, Gross Pay.
            using (StreamWriter sw = new StreamWriter(Program.FILES_LOCATION + "TopFifteenPercent.TXT"))
            {
                foreach (Employee e in empDBCopy)
                {
                    sw.WriteLine(e.ToFifteenPercentReportString());
                }
                //CloseFile
                sw.Close();
            }
        }
       
        public void GetAllStatesTaxesPaid()
        {
            //Group by State
            foreach (string s in STATE_TAX_PERCENTAGES.Keys)
            {
                StatesDB.Add(new State(s, EmployeesDB, PaycheckDB));
            }

            StatesDB = StatesDB.OrderBy(s => s.StateName).ToList();

            //Output State, Median Time Worked, Median Net Pay, State Taxes Totaled, orderd by state Alphabetically.
            using (StreamWriter sw = new StreamWriter(Program.FILES_LOCATION + "StateTaxesPaidByState.TXT"))
            {
                foreach (State s in StatesDB)
                {
                    sw.WriteLine(s.ToString());
                }
                //CloseFile
                sw.Close();
            }
        }
    }

    public class Paycheck
    {
        public int FEDERAL_TAX_PERCENT_WITHHELD = 15;
        public double FEDERAL_TAX_PERCENT_WITHHELD_TIMES = .15;
        public Dictionary<string, double> STATE_TAX_PERCENTAGES = new Dictionary<string, double>()
        {
            {"UT",5},{"WY", 5},{"NV",5},
            {"CO",6.5},{"ID",6.5},{"AZ",6.5},{"OR",6.5},
            {"WA",7},{"NM", 7},{"TX",7}
        };
        public Dictionary<string, double> STATE_TAX_PERCENTAGES_TIMES = new Dictionary<string, double>()
        {
            {"UT",.05},{"WY", .05},{"NV",.05},
            {"CO",.065},{"ID",.065},{"AZ",.065},{"OR",.065},
            {"WA",.07},{"NM", .07},{"TX",.07}
        };

        //Not using full DB, so this is a little messy. Did explicit copy of data instead of Joins between Employee and Paycheck...
        //Just so you know i'm not aware of how to normalize...
        public string EmployeeId { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public double GrossPay { get; set; }
        public double FederalTax { get; set; }
        public double StateTax { get; set; }
        public double NetPay { get; set; }
        public string State { get; set; }

        public Paycheck(Employee e)
        {
            EmployeeId = e.EmpNum;
            First = e.First;
            Last = e.Last;
            GrossPay = e.GrossPay;
            State = e.State;

            //FederalTax
            FederalTax = CalculateFederalTax();

            //StateTax
            StateTax = CalculateStateTax();

            //NetPay
            NetPay = CalculateNetPay();
        }

        public double CalculateFederalTax()
        {
            return GrossPay * FEDERAL_TAX_PERCENT_WITHHELD_TIMES;
        }

        public double CalculateStateTax()
        {
            double stateTax;
            STATE_TAX_PERCENTAGES_TIMES.TryGetValue(State, out stateTax);
            return GrossPay * stateTax;
        }

        public double CalculateNetPay()
        {
            return GrossPay - FederalTax - StateTax;
        }

        //ToString override also.
        //The output should be: employee id, first name, last name, gross pay, federal tax, state tax, net pay
        public override string ToString()
        {
            return EmployeeId + ", " + First + ", " + Last + ", " + GrossPay.ToString("N2") + ", " + FederalTax.ToString("N2") + ", " + StateTax.ToString("N2") + ", " + NetPay.ToString("N2");
        }
    }
    
    public class State
    {
        //The output should be state, median time worked, median net pay, state taxes
        public string StateName { get; set; }
        public int MedianTimeWorked { get; set; }
        public double MedianNetPay { get; set; }
        public double TotalStateTaxes { get; set; }
        int OddOrEvenModifier = 0;

        public State(string state, List<Employee> EmployeesDB, List<Paycheck> PaycheckDB)
        {
            //State being Processed.
            StateName = state;

            //Need to make copy of emp DB and paycheck and then peel out to just the state and go from there
            //Copy Database as to not disturb original order
            List<Employee> empDBCopy = new List<Employee>();
            empDBCopy.AddRange(EmployeesDB.FindAll(e=>e.State.Equals(StateName)));
            //Copy Database as to not disturb original order
            List<Paycheck> pcDBCopy = new List<Paycheck>();
            pcDBCopy.AddRange(PaycheckDB.FindAll(e => e.State.Equals(StateName)));

            MedianTimeWorked = CalculateMedianTimeWorked(empDBCopy);

            MedianNetPay = CalculateMedianNetPay(pcDBCopy);

            TotalStateTaxes = CalculateTotalStateTaxes(pcDBCopy);
        }

        public int CalculateMedianTimeWorked(List<Employee> EmployeeDB)
        {
            if (EmployeeDB.Count % 2 == 1)
            {
                OddOrEvenModifier = 1;
            }
            else
            {
                OddOrEvenModifier = 0;
            }

            //Median Time Worked
            EmployeeDB = EmployeeDB.OrderBy(e => e.HoursWorked).ToList();

            return EmployeeDB.ElementAt(EmployeeDB.Count / 2 + OddOrEvenModifier).HoursWorked;
        }

        public double CalculateMedianNetPay(List<Paycheck> PaycheckDB)
        {
            if (PaycheckDB.Count % 2 == 1)
            {
                OddOrEvenModifier = 1;
            }
            else
            {
                OddOrEvenModifier = 0;
            }

            //Median Net Pay
            PaycheckDB = PaycheckDB.OrderBy(p => p.NetPay).ToList();

            return PaycheckDB.ElementAt(PaycheckDB.Count / 2 + OddOrEvenModifier).NetPay;
        }

        public double CalculateTotalStateTaxes(List<Paycheck> PaycheckDB)
        {
            return PaycheckDB.Sum(p => p.StateTax);
        }

        //To String for State Output.
        //The output should be state, median time worked, median net pay, state taxes
        public override string ToString()
        {
            return StateName + ", " + MedianTimeWorked + ", " + MedianNetPay.ToString("N2") + ", " + TotalStateTaxes.ToString("N2");
        }
    }
}
