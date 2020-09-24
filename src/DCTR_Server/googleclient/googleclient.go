package googleclient

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"

	"github.com/degid/DCTR_Server/googleclient"
	"golang.org/x/net/context"
	"golang.org/x/oauth2"
	"golang.org/x/oauth2/google"
	"google.golang.org/api/drive/v3"
)

// Googfi for colect files
var Googfi map[string]*GoogleFile

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

// GetClient - retrieve a token, saves the token, then returns the generated client.
func GetClient(config *oauth2.Config) *http.Client {
	tokFile := "token.json"
	tok, err := tokenFromFile(tokFile)
	if err != nil {
		tok = getTokenFromWeb(config)
		saveToken(tokFile, tok)
	}
	return config.Client(context.Background(), tok)
}

// Request a token from the web, then returns the retrieved token.
func getTokenFromWeb(config *oauth2.Config) *oauth2.Token {
	authURL := config.AuthCodeURL("state-token", oauth2.AccessTypeOffline)
	log.Print("Show link with authorization code")
	fmt.Println("Go to the following link in your browser then type the "+
		"authorization code: \n%v\n", authURL)

	var authCode string
	log.Print("Wite token...")
	if _, err := fmt.Scan(&authCode); err != nil {
		log.Fatalf("Unable to read authorization code %v", err)
	}

	tok, err := config.Exchange(context.TODO(), authCode)
	if err != nil {
		log.Fatalf("Unable to retrieve token from web %v", err)
	}
	return tok
}

// Retrieves a token from a local file.
func tokenFromFile(file string) (*oauth2.Token, error) {
	f, err := os.Open(file)
	if err != nil {
		return nil, err
	}
	defer f.Close()
	tok := &oauth2.Token{}
	err = json.NewDecoder(f).Decode(tok)
	return tok, err
}

// Saves a token to a file path.
func saveToken(path string, token *oauth2.Token) {
	fmt.Printf("Saving credential file to: %s\n", path)
	f, err := os.OpenFile(path, os.O_RDWR|os.O_CREATE|os.O_TRUNC, 0600)
	if err != nil {
		log.Fatalf("Unable to cache oauth token: %v", err)
	}
	defer f.Close()
	json.NewEncoder(f).Encode(token)
	log.Print("Save token.")
}

// GetGoogleFiles - get files with stats servers from Google Drive
func GetGoogleFiles() {
	Googfi = make(map[string]*GoogleFile, 0)

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

	log.Print("Loading are files from google drive...")
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
			//gf[i.Id] = NewFile(i.Id, i.Name, i.ModifiedTime)
			//x++
		}
		//fmt.Println("Delete", x)
	}
	log.Print("Completed.")
}
