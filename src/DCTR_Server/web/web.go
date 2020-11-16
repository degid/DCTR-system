package web

import (
	"context"
	"html/template"
	"log"
	"net/http"
	"os"
	"os/signal"
	"sync/atomic"
	"syscall"
	"time"
)

// InitGoogleDisk checks the readiness of google drive
// and provides an authorization process
type InitGoogleDisk struct {
	status  bool
	code    string
	authURL string
}

func (disk *InitGoogleDisk) getStatus() bool {
	return disk.status
}

func (disk *InitGoogleDisk) setStatus(status bool) {
	disk.status = status
}

func (disk *InitGoogleDisk) setCode(code string) {
	disk.code = code
}

func (disk *InitGoogleDisk) getCode() string {
	return disk.code
}

func (disk *InitGoogleDisk) isCodeEmpty() (rez bool) {
	if disk.getCode() == "" {
		rez = true
	} else {
		rez = false
	}
	return
}

func (disk *InitGoogleDisk) setURL(url string) {
	disk.authURL = url
}

func (disk *InitGoogleDisk) getURL() string {
	return disk.authURL
}

var initDisk InitGoogleDisk

// healthz is a liveness probe.
func healthz(w http.ResponseWriter, _ *http.Request) {
	w.WriteHeader(http.StatusOK)
}

// readyz is a readiness probe.
func readyz(isReady *atomic.Value) http.HandlerFunc {
	return func(w http.ResponseWriter, _ *http.Request) {
		if isReady == nil || !isReady.Load().(bool) {
			http.Error(w, http.StatusText(http.StatusServiceUnavailable), http.StatusServiceUnavailable)
			return
		}
		w.WriteHeader(http.StatusOK)
	}
}

func indexHandler(w http.ResponseWriter, r *http.Request) {
	t, err := template.ParseFiles("templates/index.html",
		"templates/body.html", "templates/footer.html",
		"templates/header.html", "templates/head.html")
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}

	if initDisk.getStatus() == false {
		http.Redirect(w, r, "/gettoken", 302)
	}

	err = t.ExecuteTemplate(w, "index", initDisk.getStatus())
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}
}

func saveCodeHandler(w http.ResponseWriter, r *http.Request) {
	code := r.FormValue("inputcode")
	if code != "" {
		initDisk.setCode(code)
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

	err = t.ExecuteTemplate(w, "setparam", initDisk.getURL())
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

	if okcode == "" || initDisk.getStatus() {
		http.Redirect(w, r, "/", 302)
	}

	err = t.ExecuteTemplate(w, "waitservice", okcode)
	if err != nil {
		log.Fatalf("%s: %v", w, err.Error())
	}
}

func chekCode(param *<-chan bool, AuthURL *<-chan string, AuthCode *chan<- string) {
	if <-*param != true {
		initDisk.setStatus(false)
		initDisk.setURL(<-*AuthURL)

		for initDisk.isCodeEmpty() {
			time.Sleep(2 * time.Millisecond)
		}

		*AuthCode <- initDisk.getCode()

		if <-*param {
			log.Println("Web get response!")
		}
	}
	initDisk.setStatus(true)
}

// Start - Start web service interface
func Start(param <-chan bool, AuthURL <-chan string, AuthCode chan<- string, done chan<- struct{}) {
	isReady := &atomic.Value{}
	isReady.Store(false)
	port := os.Getenv("PORT")
	go func() {
		log.Printf("Readyz probe is negative by default...")
		//time.Sleep(10 * time.Second)
		if port == "" {
			log.Fatal("Port is not set.")
			isReady.Store(false)
		} else {
			isReady.Store(true)
			log.Printf("Readyz probe is positive.")
		}
	}()

	log.Print("Starting the service...")

	go chekCode(&param, &AuthURL, &AuthCode)

	http.Handle("/assets/", http.StripPrefix("/assets/", http.FileServer(http.Dir("./templates/assets/"))))
	http.HandleFunc("/", indexHandler)
	http.HandleFunc("/waitservice", waitServiceHandler)
	http.HandleFunc("/gettoken", getCodeHandler)
	http.HandleFunc("/savecode", saveCodeHandler)
	http.HandleFunc("/healthz", healthz)
	http.HandleFunc("/readyz", readyz(isReady))

	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt, syscall.SIGTERM)

	websrv := &http.Server{
		Addr:    "0.0.0.0:" + port,
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
