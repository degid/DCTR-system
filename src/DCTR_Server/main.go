package main

import (
	"fmt"
	"html/template"
	"io/ioutil"
	"log"
	"net/http"

	"github.com/degid/DCTR-system/src/DCTR_Server/googleclient"

	"golang.org/x/oauth2/google"
	"google.golang.org/api/drive/v3"
)

// GoogleFile for google file
type GoogleFile struct {
	ID           string
	Name         string
	ModifiedTime string
}

// NewFile for read file from google.drive
func NewFile(id, name, mTime string) *GoogleFile {
	return &GoogleFile{id, name, mTime}
}

var gf map[string]*GoogleFile

func indexHandler(w http.ResponseWriter, r *http.Request) {
	t, err := template.ParseFiles("templates/index.html",
		"templates/head.html", "templates/body.html", "templates/footer.html")
	if err != nil {
		fmt.Fprintf(w, err.Error())
	}

	fmt.Println("WHAT:", gf)

	err = t.ExecuteTemplate(w, "index", gf)
	if err != nil {
		fmt.Println("ERRR1:", err)
	}
}

func main() {
	gf = make(map[string]*GoogleFile, 0)

	b, err := ioutil.ReadFile("client_secret.json")
	if err != nil {
		log.Fatalf("Unable to read client secret file: %v", err)
	}

	// If modifying these scopes, delete your previously saved token.json.
	config, err := google.ConfigFromJSON(b, drive.DriveFileScope)
	if err != nil {
		log.Fatalf("Unable to parse client secret file to config: %v", err)
	}

	client := googleclient.GetClient(config)

	srv, err := drive.New(client)
	if err != nil {
		log.Fatalf("Unable to retrieve Drive client: %v", err)
	}

	r, err := srv.Files.List().
		Q("'1BLNchJo-RZK7aeFiyfqb8FWUgbmpRLjz' in parents").
		Fields("nextPageToken, files(id, name, modifiedTime)").PageSize(20).
		Do()

	if err != nil {
		log.Fatalf("Unable to retrieve files: %v", err)
	}

	//x := 0
	fmt.Println("Files:")
	if len(r.Files) == 0 {
		fmt.Println("No files found.")
	} else {
		for _, i := range r.Files {
			//err := srv.Files.Delete(i.Id).Do()
			//if err != nil {
			//	fmt.Println(err)
			//	break
			//}

			fmt.Printf("%s (%s) [%s]\n", i.Name, i.Id, i.ModifiedTime)
			gf[i.Id] = NewFile(i.Id, i.Name, i.ModifiedTime)
			//x++
		}
		//fmt.Println("Delete", x)
	}

	http.Handle("/assets/", http.StripPrefix("/assets/", http.FileServer(http.Dir("./templates/assets/"))))
	http.HandleFunc("/", indexHandler)
	http.ListenAndServe(":3000", nil)
}
