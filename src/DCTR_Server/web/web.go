package web

import (
	"context"
	"fmt"
	"html/template"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
)

func indexHandler(w http.ResponseWriter, r *http.Request) {
	t, err := template.ParseFiles("templates/index.html",
		"templates/body.html", "templates/footer.html",
		"templates/header.html", "templates/head.html")
	if err != nil {
		fmt.Fprintf(w, err.Error())
	}

	err = t.ExecuteTemplate(w, "index", nil)
	if err != nil {
		fmt.Println("Error:", err)
	}
}

// Startweb - Start web service interface
func Startweb(done chan<- struct{}) {
	log.Print("Starting the service...")
	port := os.Getenv("PORT")
	if port == "" {
		log.Fatal("Port is not set.")
	}

	http.Handle("/assets/", http.StripPrefix("/assets/", http.FileServer(http.Dir("./templates/assets/"))))
	http.HandleFunc("/", indexHandler)

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

	//http.ListenAndServe(":"+port, nil)
}
