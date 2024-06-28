using Soneta.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Types;
using Rekrutacja.Workers.Template;
using Soneta.Tools;
using Soneta.Core.Extensions;
using Soneta.Data.QueryDefinition;

//Rejetracja Workera - Pierwszy TypeOf określa jakiego typu ma być wyświetlany Worker, Drugi parametr wskazuje na jakim Typie obiektów będzie wyświetlany Worker
[assembly: Worker(typeof(TemplateWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers.Template
{
    public class TemplateWorker
    {
        public enum OperationSign //INFO deklaracja dozwolonych znaków w oknie Operacja
        {
            [Caption("+")]
            Addition,
            [Caption("-")]
            Subtraction,
            [Caption("*")]
            Multiplication,
            [Caption("/")]
            Division
        }

        //Aby parametry działały prawidłowo dziedziczymy po klasie ContextBase
        public class TemplateWorkerParametry : ContextBase
        {
            [Caption("A"), Priority(1)]
            public double ParamA { get; set; }
            [Caption("B"), Priority(2)]
            public double ParamB { get; set; }

            [Caption("Data obliczeń"), Priority(3)]
            public Date DataObliczen { get; set; }
            [Caption("Operacja"), Priority(4)]
            public OperationSign Sign { get; set; }
            public TemplateWorkerParametry(Context context) : base(context)
            {
                ParamA = 0;
                ParamB = 0;
                this.DataObliczen = Date.Today;
                Sign = OperationSign.Addition;
            }
        }
        //Obiekt Context jest to pudełko które przechowuje Typy danych, aktualnie załadowane w aplikacji
        //Atrybut Context pobiera z "Contextu" obiekty które aktualnie widzimy na ekranie
        [Context]
        public Context Cx { get; set; }
        //Pobieramy z Contextu parametry, jeżeli nie ma w Context Parametrów mechanizm sam utworzy nowy obiekt oraz wyświetli jego formatkę
        [Context]
        public TemplateWorkerParametry Parametry { get; set; }
        //Atrybut Action - Wywołuje nam metodę która znajduje się poniżej
        [Action("Kalkulator",
           Description = "Prosty kalkulator ",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]
        public void WykonajAkcje()
        {
            //Włączenie Debug, aby działał należy wygenerować DLL w trybie DEBUG
            DebuggerSession.MarkLineAsBreakPoint();
            //Pobieranie danych z Contextu
            Pracownik[] pracownicy = null; //INFO zakładam że można zaznaczyć wiecej niż jeden rekord (interface na to pozwala) 
            if (this.Cx.Contains(typeof(Pracownik[]))) 
            {
                pracownicy = (Pracownik[])this.Cx[typeof(Pracownik[])];
            }

            if((pracownicy == null) || (pracownicy.Length == 0)) //INFO sprawdzam czy jest cokolwiek (chociaż interface nie pozwala na nie zaznaczenie rekordu)
            {
                throw new InvalidOperationException("You must select at least one record.");
            }

            //Modyfikacja danych
            //Aby modyfikować dane musimy mieć otwartą sesję, któa nie jest read only
            using (Session nowaSesja = this.Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                //Otwieramy Transaction aby można było edytować obiekt z sesji
                using (ITransaction trans = nowaSesja.Logout(true))
                {
                    var result = Calculate(); //INFO dla każdego rekordu te same parametry, oblicznie wyniku tylko raz
                    foreach (var pracownik in pracownicy) //INFO wprowadzanie zmian w każdym zaznaczonym rekordzie
                    {
                        //Pobieramy obiekt z Nowo utworzonej sesji
                        var pracownikZSesja = nowaSesja.Get(pracownik);
                        //Features - są to pola rozszerzające obiekty w bazie danych, dzięki czemu nie jestesmy ogarniczeni to kolumn jakie zostały utworzone przez producenta
                        pracownikZSesja.Features["DataObliczen"] = this.Parametry.DataObliczen;
                        pracownikZSesja.Features["Wynik"] = result;
                    }
                   
                    //Zatwierdzamy zmiany wykonane w sesji
                    trans.CommitUI();
                }
                //Zapisujemy zmiany
                nowaSesja.Save();
            }
        }

        public double Calculate() //INFO oblicznie wyniku kalkulacji
        {
            switch (Parametry.Sign)
            {
                case OperationSign.Addition:
                    return Parametry.ParamA + Parametry.ParamB;
 
                case OperationSign.Subtraction:
                    return Parametry.ParamA - Parametry.ParamB;

                case OperationSign.Multiplication:
                    return Parametry.ParamA * Parametry.ParamB;

                case OperationSign.Division:
                    if (Parametry.ParamB == 0) //INFO wyłapywanie niedozwolonej operacji
                    {
                        throw new InvalidOperationException("You can't divide by zero.");
                    }
                    else
                    {
                        return Parametry.ParamA / Parametry.ParamB;
                    }
                default:
                    throw new InvalidOperationException("Invalid operation sign.");
            }
        }
    }
}