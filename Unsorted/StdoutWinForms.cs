static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // Bind stdout to a specific output stream (like a file or process redirection)
        StreamWriter stdOut = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };
        StreamWriter stdErr = new StreamWriter(Console.OpenStandardError())
        {
            AutoFlush = true
        };
        Console.SetError(stdErr);
        Console.SetOut(stdOut);

        try
        {
            Console.WriteLine("...");

            return 0;
        }
        catch (Exception ex)
        {
            // General error handling block
            string errorMessage = "An unexpected error occurred: " + ex.Message;
            Console.Error.WriteLine(errorMessage);

            return 1;
        }
        finally
        {
            stdOut.Close();
            stdErr.Close();
        }
    }
}
