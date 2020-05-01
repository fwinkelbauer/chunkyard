(defun exec-app (application args valid-exit-codes)
  "Runs an `application' with a list of `args' using
`shell-command' and returns a `user-error' if the exit code is
not a member of the `valid-exit-codes' list"
  (let ((shell-command-dont-erase-buffer t)
        (full-cmd (mapconcat #'identity (add-to-list 'args application) " ")))
    (let ((exit-code (shell-command full-cmd "*eci*")))
      (unless (member exit-code valid-exit-codes)
        (user-error "Exit code of application was %s" exit-code)))))

(defun dotnet (&rest args)
  "Calls the dotnet binary"
  (exec-app "dotnet" args '(0)))

(defun to-file (&rest args)
  "Joins `args' into a single path using `expand-file-name'.
Afterwards it transforms the delimeters ('\\' and '/') according
to the current OS"
  (let ((expanded (expand-file-name (mapconcat #'identity args "/"))))
    (if (eq system-type 'windows-nt)
        (subst-char-in-string ?/ ?\\ expanded)
      expanded)))

(defun run-completing-read (tasks)
  (funcall (intern (completing-read "Run: " tasks))))



(setq solution-file (to-file "Chunkyard.sln")
      main-project (to-file "Chunkyard/Chunkyard.csproj")
      test-project (to-file "Chunkyard.Tests/Chunkyard.Tests.csproj")
      build-configuration "Release"
      build-runtime "win-x64")

(setq artifacts-directory (to-file "artifacts")
      build-directory (to-file artifacts-directory "build"))

(defun clean-task()
  "Cleans the solution"
  (message "Cleaning solution")
  (delete-directory artifacts-directory t)
  (dotnet "clean" solution-file))

(defun build-task ()
  "Builds the solution"
  (message "Building solution")
  (dotnet "build" solution-file
          "-c" build-configuration)
  (dotnet "publish" main-project
          "-c" build-configuration
          "-r" build-runtime
          "-o" build-directory))

(defun test-task ()
  "Runs all tests in the solution"
  (message "Running all tests")
  (dotnet "test" solution-file
          "-c" build-configuration))

(defun ci-task ()
  "Runs `clean-task', `build-task' and `test-task'"
  (clean-task)
  (build-task)
  (test-task))

(defun run-custom-task ()
  "Lets the user choose a task to start"
  (interactive)
  (run-completing-read
   '(clean-task build-task test-task ci-task)))

(run-custom-task)
