using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPacifier.BackEnd.AlgorithmLayer
{
    public class DataForwarder
    {
        private readonly PythonScriptEngine _pythonScriptEngine;
        private readonly string _scriptPath;

        public DataForwarder(string scriptPath)
        {
            _pythonScriptEngine = new PythonScriptEngine();
            _scriptPath = scriptPath;
        }

        public async Task<string> ForwardToPythonAsync(string dataJson)
        {
            if (string.IsNullOrWhiteSpace(dataJson))
            {
                throw new ArgumentException("Data JSON cannot be null or empty", nameof(dataJson));
            }

            string response = await _pythonScriptEngine.ExecuteScriptAsync(_scriptPath, dataJson);
            return response;
        }
    }

}
