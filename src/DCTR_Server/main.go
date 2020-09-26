package main

import (
	"net/http"

	"github.com/degid/DCTR-system/src/DCTR_Server/googleclient"
	"github.com/degid/DCTR-system/src/DCTR_Server/web"
)

func main() {
	//gf := make(chan *googleclient.GoogleFile, 100)
	paramOk := make(chan bool)
	AuthURL := make(chan string)
	AuthCode := make(chan string)
	client := make(chan *http.Client)

	go googleclient.GetClient(paramOk, AuthURL, AuthCode, client)

	//go googleclient.GetGoogleFiles(client, gf)

	chstop := make(chan struct{})
	go web.Start(paramOk, AuthURL, AuthCode, chstop)

	<-chstop
}
