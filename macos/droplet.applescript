on run
	display dialog "Arraste um index.html sobre o Fenestra, ou use Abrir com." buttons {"OK"} default button "OK" with title "Fenestra"
end run

on open theFiles
	repeat with f in theFiles
		try
			set p to POSIX path of f
			set exe to POSIX path of (path to resource "fenestra")
			do shell script quoted form of exe & " " & quoted form of p & " > /dev/null 2>&1 &"
		on error errMsg
			display dialog "Nao consegui abrir: " & errMsg buttons {"OK"} default button "OK" with title "Fenestra"
		end try
	end repeat
end open
