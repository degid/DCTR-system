package web

import (
	"context"
	"html/template"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"
)

var paramToken map[string]string

func indexHandler(w http.ResponseWriter, r *http.Request) {
	t, err := template.ParseFiles("templates/index.html",
		"templates/body.html", "templates/footer.html",
		"templates/header.html", "templates/head.html")
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}

	if value, inMap := paramToken["ok"]; !inMap {
		http.Redirect(w, r, "/waitservice", 302)
	} else if value == "0" {
		http.Redirect(w, r, "/gettoken", 302)
	}

	err = t.ExecuteTemplate(w, "index", paramToken["ok"])
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}
}

func saveCodeHandler(w http.ResponseWriter, r *http.Request) {
	code := r.FormValue("inputcode")
	if code != "" {
		paramToken["code"] = code
		http.Redirect(w, r, "/waitservice?code=1", 302)
	} else {
		http.Redirect(w, r, "/gettoken", 302)
	}
}

func getCodeHandler(w http.ResponseWriter, r *http.Request) {
	t, err := template.ParseFiles("templates/setparam.html",
		"templates/footer.html",
		"templates/header.html", "templates/head.html")
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}

	err = t.ExecuteTemplate(w, "setparam", paramToken["authURL"])
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}
}

func waitServiceHandler(w http.ResponseWriter, r *http.Request) {
	t, err := template.ParseFiles("templates/waitservice.html",
		"templates/footer.html", "templates/header.html", "templates/head.html")
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}

	okcode := r.FormValue("code")

	if value, inMap := paramToken["ok"]; inMap && okcode == "" || value == "1" {
		http.Redirect(w, r, "/", 302)
	}

	err = t.ExecuteTemplate(w, "waitservice", okcode)
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}
}

func chekCode(param *<-chan bool, AuthURL *<-chan string, AuthCode *chan<- string) {
	paramToken = make(map[string]string)
	if <-*param != true {
		paramToken["ok"] = "0"
		paramToken["authURL"] = <-*AuthURL

		for paramToken["code"] == "" {
			time.Sleep(2 * time.Millisecond)
		}

		*AuthCode <- paramToken["code"]

		if <-*param {
			log.Println("Web get response!")
		}
	}
	paramToken["ok"] = "1"
}

// Start - Start web service interface
func Start(param <-chan bool, AuthURL <-chan string, AuthCode chan<- string, done chan<- struct{}) {
	log.Print("Starting the service...")
	port := os.Getenv("PORT")
	if port == "" {
		log.Fatal("Port is not set.")
	}

	go chekCode(&param, &AuthURL, &AuthCode)

	http.Handle("/assets/", http.StripPrefix("/assets/", http.FileServer(http.Dir("./templates/assets/"))))
	http.HandleFunc("/", indexHandler)
	http.HandleFunc("/waitservice", waitServiceHandler)
	http.HandleFunc("/gettoken", getCodeHandler)
	http.HandleFunc("/savecode", saveCodeHandler)

	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt, syscall.SIGTERM)

	websrv := &http.Server{
		Addr:    ":" + port,
		Handler: nil,
	}

	go func() {
		log.Fatal(websrv.ListenAndServe())
	}()
	log.Print("The service is ready to listen and serve.")

	killSignal := <-interrupt
	switch killSignal {
	case os.Interrupt:
		log.Print("sigint...")
	case syscall.SIGTERM:
		log.Print("sigterm...")
	}

	log.Print("The service is shutting down...")
	websrv.Shutdown(context.Background())
	log.Print("Done")

	close(done)
}
