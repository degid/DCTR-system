package main

import (
	"fmt"
	"google"
	"io/ioutil"
	"log"
	"net/http"

	"golang.org/x/oauth2/google"
	"google.golang.org/api/drive/v3"
)

func main() {
	b, err := ioutil.ReadFile("client_secret.json")
	if err != nil {
		log.Fatalf("Unable to read client secret file: %v", err)
	}

	// If modifying these scopes, delete your previously saved token.json.
	config, err := google.ConfigFromJSON(b, drive.DriveFileScope)
	if err != nil {
		log.Fatalf("Unable to parse client secret file to config: %v", err)
	}
	client := getClient(config)

	srv, err := drive.New(client)
	if err != nil {
		log.Fatalf("Unable to retrieve Drive client: %v", err)
	}

	r, err := srv.Files.List().
		Q("'1WWfOtUJ2DaMVUEMiIjKqx--dL2sriX1X' in parents").
		Fields("nextPageToken, files(id, name, modifiedTime)").PageSize(800).
		Do()

	if err != nil {
		log.Fatalf("Unable to retrieve files: %v", err)
	}

	x := 0
	fmt.Println("Files:")
	if len(r.Files) == 0 {
		fmt.Println("No files found.")
	} else {
		for _, i := range r.Files {
			err := srv.Files.Delete(i.Id).Do()
			if err != nil {
				fmt.Println(err)
				break
			}

			fmt.Printf("%s (%s) [%s]\n", i.Name, i.Id, i.ModifiedTime)
			x++
		}
		fmt.Println("Delete", x)
	}

}

func main2() {
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "Hello World!")
	})
	http.ListenAndServe(":80", nil)
}
