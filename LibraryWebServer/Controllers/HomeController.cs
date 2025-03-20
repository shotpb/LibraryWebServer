using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Diagnostics;
using System.Dynamic;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

[assembly: InternalsVisibleTo( "TestProject1" )]
namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {

        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).
        private static string user = "";
        private static int card = -1;

        private readonly ILogger<HomeController> _logger;


        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin( string name, int cardnum )
        {
            // TODO: Fill in. Determine if login is successful or not.
            bool loginSuccessful = false;

            using (Team122LibraryContext db = new Team122LibraryContext())
            {
                var query =
                from p in db.Patrons
                where p.Name == name && p.CardNum == cardnum
                select p;

                if ( query.Any() )
                    loginSuccessful = true;

            }

            if ( !loginSuccessful )
            {
                return Json( new { success = false } );
            }
            else
            {
                user = name;
                card = cardnum;
                return Json( new { success = true } );
            }
        }


        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";
            card = -1;
            return Json( new { success = true } );
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {

            // TODO: Implement
            using (Team122LibraryContext db = new Team122LibraryContext())
            {
                var query =
                from t in db.Titles
                join i in db.Inventory
                on t.Isbn equals i.Isbn into title

                from t1 in title.DefaultIfEmpty()
                join c in db.CheckedOut
                on t1.Serial equals c.Serial into check

                from c1 in check.DefaultIfEmpty()
                join p in db.Patrons
                on c1.CardNum equals p.CardNum into allInfo

                from j in allInfo.DefaultIfEmpty()
                select new {
                    isbn = t.Isbn, 
                    title = t.Title, 
                    author = t.Author, 
                    serial = t1 == null ? null : (uint?)t1.Serial, 
                    name = j == null ? "" : j.Name };

                return Json(query.ToArray());
            }
        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {
            using (Team122LibraryContext db = new Team122LibraryContext())
            {
                var query =
                from c in db.CheckedOut where c.CardNum == card
                join i in db.Inventory
                on c.Serial equals i.Serial into inv 

                from j1 in inv.DefaultIfEmpty()
                join t in db.Titles
                on j1.Isbn equals t.Isbn into titles

                from t1 in titles.DefaultIfEmpty()
                select new
                {
                    title = t1.Title,
                    author = t1.Author,
                    serial = c.Serial
                };

                return Json(query.ToArray());
            }
        }


        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook( int serial )
        {
            // You may have to cast serial to a (uint)
            using (Team122LibraryContext db = new Team122LibraryContext())
            {
                try
                {
                    CheckedOut checkedOut = new CheckedOut();
                    checkedOut.Serial = (uint)serial;
                    checkedOut.CardNum = (uint)card;

                    db.CheckedOut.Add(checkedOut);
                    db.SaveChanges();
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error checking out book: " + e.Message);
                }
                return Json(new { success = true });
            }
        }

        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook( int serial )
        {
            // You may have to cast serial to a (uint)
            using (Team122LibraryContext db = new Team122LibraryContext())
            {
                try
                {
                    CheckedOut checkedOut = new CheckedOut();
                    checkedOut.Serial = (uint)serial;
                    checkedOut.CardNum = (uint)card;

                    db.CheckedOut.Remove(checkedOut);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error checking in book: " + e.Message);
                }
                return Json(new { success = true });
            }
        }


        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/


        public IActionResult Index()
        {
            if ( user == "" && card == -1 )
                return View( "Login" );

            return View();
        }


        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if ( user == "" && card == -1 )
                return View( "Login" );

            return View();
        }

        public HomeController( ILogger<HomeController> logger )
        {
            _logger = logger;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache( Duration = 0, Location = ResponseCacheLocation.None, NoStore = true )]
        public IActionResult Error()
        {
            return View( new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier } );
        }
    }
}