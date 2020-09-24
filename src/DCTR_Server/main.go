package main

import (
	"github.com/degid/DCTR-system/src/DCTR_Server/web"
)

func main() {

	chstop := make(chan struct{})
	go web.Startweb(chstop)

	<-chstop
}
