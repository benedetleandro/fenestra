package main

import (
	_ "embed"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"time"
)

//go:embed update.html
var updateHTML []byte

type updateInfo struct {
	Version  string `json:"version"`
	Notes    string `json:"notes"`
	Download string `json:"download"`
	Manual   string `json:"manual"`
	Sha256   string `json:"sha256"`
}

var currentUpdate *updateInfo
var updateChecked bool

const updateCheckDays = 7

func checkForUpdates(force bool) *updateInfo {
	if updateChecked && !force {
		return currentUpdate
	}
	updateChecked = true
	if force {
		currentUpdate = nil
	} else {
		if os.Getenv("FENESTRA_NO_UPDATE_TAB") == "1" || os.Getenv("FENESTRA_NO_OPEN") == "1" {
			return nil
		}
		if last, err := os.ReadFile(filepath.Join(configDir(), "update-check")); err == nil {
			if when, err := time.Parse(time.RFC3339, strings.TrimSpace(string(last))); err == nil {
				if time.Since(when) < updateCheckDays*24*time.Hour {
					return nil
				}
			}
		}
	}

	_ = os.MkdirAll(configDir(), 0o750)
	_ = os.WriteFile(filepath.Join(configDir(), "update-check"), []byte(time.Now().UTC().Format(time.RFC3339)), 0o600)

	url := os.Getenv("FENESTRA_UPDATE_URL")
	if url == "" {
		url = "https://benedetleandro.github.io/fenestra/version.json"
	}

	client := &http.Client{Timeout: 3 * time.Second}
	req, err := http.NewRequest(http.MethodGet, url, nil) // #nosec G704 -- fixed project URL, env override only for tests
	if err != nil {
		logf("update check: %v", err)
		return nil
	}
	req.Header.Set("User-Agent", "Fenestra/"+version)
	resp, err := client.Do(req) // #nosec G704 -- fixed project URL, env override only for tests
	if err != nil {
		logf("update check: %v", err)
		return nil
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		return nil
	}

	var info updateInfo
	if err := json.NewDecoder(io.LimitReader(resp.Body, 1<<20)).Decode(&info); err != nil {
		logf("update check: %v", err)
		return nil
	}
	if info.Version == "" || compareVersions(info.Version, version) <= 0 {
		return nil
	}
	currentUpdate = &info
	logf("update available: %s", info.Version)
	return currentUpdate
}

func compareVersions(a, b string) int {
	as := strings.Split(a, ".")
	bs := strings.Split(b, ".")
	n := len(as)
	if len(bs) > n {
		n = len(bs)
	}
	for i := 0; i < n; i++ {
		var ai, bi int
		if i < len(as) {
			fmt.Sscanf(as[i], "%d", &ai)
		}
		if i < len(bs) {
			fmt.Sscanf(bs[i], "%d", &bi)
		}
		if ai != bi {
			if ai < bi {
				return -1
			}
			return 1
		}
	}
	return 0
}

func checkUpdateCommand() {
	info := checkForUpdates(true)
	if info == nil {
		fmt.Println("Fenestra " + version + " is up to date.")
		return
	}
	fmt.Println("Fenestra " + info.Version + " is available (you are running " + version + ").")
	if info.Notes != "" {
		fmt.Println("Notes:   " + info.Notes)
	}
	if info.Download != "" {
		fmt.Println("Download: " + info.Download)
	}
	if info.Sha256 != "" {
		fmt.Println("SHA-256:  " + info.Sha256)
	}
}

func maybeOpenUpdateTab(port int) {
	if currentUpdate == nil {
		return
	}
	if os.Getenv("FENESTRA_NO_UPDATE_TAB") == "1" || os.Getenv("FENESTRA_NO_OPEN") == "1" {
		return
	}
	if last, err := os.ReadFile(filepath.Join(configDir(), "update-tab")); err == nil {
		if strings.TrimSpace(string(last)) == currentUpdate.Version {
			return
		}
	}
	launch(fmt.Sprintf("http://127.0.0.1:%d/__fenestra/update", port))
	_ = os.MkdirAll(configDir(), 0o750)
	_ = os.WriteFile(filepath.Join(configDir(), "update-tab"), []byte(currentUpdate.Version), 0o600)
}

func serveUpdatePage(w http.ResponseWriter) {
	if currentUpdate == nil {
		http.Error(w, "no update available", http.StatusNotFound)
		return
	}
	page := string(updateHTML)
	repl := strings.NewReplacer(
		"{{VERSION}}", currentUpdate.Version,
		"{{CURRENT}}", version,
		"{{NOTES}}", currentUpdate.Notes,
		"{{DOWNLOAD}}", currentUpdate.Download,
		"{{SHA}}", currentUpdate.Sha256,
	)
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Header().Set("Cache-Control", "no-store")
	_, _ = io.WriteString(w, repl.Replace(page))
}
