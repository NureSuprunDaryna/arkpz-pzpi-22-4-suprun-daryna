using System.Collections;

namespace BadCodeExample
{
    public class U
    {
        public string n;
        public string e;
        public string d;

        public U(string x, string y, string z)
        {
            n = x;
            e = y;
            d = z;
        }
    }

    public class DS
    {
        public decimal calc(decimal p, string t)
        {
            Hashtable rates = new Hashtable();
            rates.Add("a", 0.9m);
            rates.Add("b", 0.85m);
            rates.Add("c", 0.8m);
            rates.Add("none", 1.0m);

            if (!rates.ContainsKey(t)) t = "none";
            return p * (decimal)rates[t];
        }
    }

    public class ES
    {
        public void Send(U u, string m)
        {
            Console.WriteLine("sending..." + u.e); Thread.Sleep(2000); Console.WriteLine("sent.");
        }
    }

    public class UR
    {
        public void SaveIt(U u)
        {
            Console.WriteLine(u.n + " saved.");
        }
    }

    public class L
    {
        public void Log(string o)
        {
            Console.WriteLine(o);
        }
    }

    public class S
    {
        DS ds = new DS();
        ES es = new ES();
        UR ur = new UR();
        L l = new L();

        public void Do(U u, decimal p)
        {
            var fp = ds.calc(p, u.d);
            l.Log("discount for " + u.n + ": " + fp);
            es.Send(u, "thanks, you paid " + fp);
            ur.SaveIt(u);
        }
    }

    class P
    {
        static void Main(string[] args)
        {
            U u = new U("User", "user@example.com", "a");
            S s = new S();
            s.Do(u, 1000m);
        }
    }
}

