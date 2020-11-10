using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Azure.AI.FormRecognizer.Training;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecognizeFormContent
{
    class Program
    {
        static void Main(string[] args)
        {
            string trainingDataUrl = "<SAS-URL-of-your-form-folder-in-blob-storage>";
            string formUrl = "<SAS-URL-of-a-form-in-blob-storage>";
            string receiptUrl = "https://docs.microsoft.com/azure/cognitive-services/form-recognizer/media"
            + "/contoso-allinone.jpg";

            var recognizerClient = AuthenticateRecognizerClient();
            var trainingClient = AuthenticateTrainingClient();

            var recognizeContent = RecognizeContent(recognizerClient);
            Task.WaitAll(recognizeContent);

            var analyzeReceipt = AnalyzeReceipt(recognizerClient, receiptUrl);
            Task.WaitAll(analyzeReceipt);

            var trainModel = TrainModel(trainingClient, trainingDataUrl);
            Task.WaitAll(trainModel);

            var analyzeForm = AnalyzePdfForm(recognizerClient, trainModel.Id, formUrl);
            Task.WaitAll(analyzeForm);

            ManageModels(trainingClient, trainingDataUrl);

        }

        const string endpoint = "<replace-with-your-form-recognizer-endpoint-here>";
        const string apiKey = "<replace-with-your-form-recognizer-key-here>";

        static private FormRecognizerClient AuthenticateRecognizerClient()
        {
           
            var credential = new AzureKeyCredential(apiKey);
            var client = new FormRecognizerClient(new Uri(endpoint), credential);
            return client;
        }

        static private FormTrainingClient AuthenticateTrainingClient()
        {
            string endpoint = "<replace-with-your-form-recognizer-endpoint-here>";
            string apiKey = "<replace-with-your-form-recognizer-key-here>";
            var credential = new AzureKeyCredential(apiKey);
            var client = new FormTrainingClient(new Uri(endpoint), credential);
            return client;
        }

        private static async Task RecognizeContent(FormRecognizerClient recognizerClient)
        {
            var invoiceUri = "https://raw.githubusercontent.com/Azure/azure-sdk-for-python/master/sdk/formrecognizer/azure-ai-formrecognizer/tests/sample_forms/forms/Invoice_1.pdf";
            FormPageCollection formPages = await recognizerClient
                .StartRecognizeContentFromUri(new Uri(invoiceUri))
                .WaitForCompletionAsync();

            foreach (FormPage page in formPages)
            {
                Console.WriteLine($"Form Page {page.PageNumber} has {page.Lines.Count} lines.");

                for (int i = 0; i < page.Lines.Count; i++)
                {
                    FormLine line = page.Lines[i];
                    Console.WriteLine($"    Line {i} has {line.Words.Count} word{(line.Words.Count > 1 ? "s" : "")}, and text: '{line.Text}'.");
                }

                for (int i = 0; i < page.Tables.Count; i++)
                {
                    FormTable table = page.Tables[i];
                    Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");
                    foreach (FormTableCell cell in table.Cells)
                    {
                        Console.WriteLine($"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains text: '{cell.Text}'.");
                    }
                }
            }
        }

        private static async Task AnalyzeReceipt(FormRecognizerClient recognizerClient, string receiptUri)
        {
            RecognizedFormCollection receipts = await recognizerClient.StartRecognizeReceiptsFromUri(new Uri(receiptUri)).WaitForCompletionAsync();
            foreach (RecognizedForm receipt in receipts)
            {
                FormField merchantNameField;
                if (receipt.Fields.TryGetValue("MerchantName", out merchantNameField))
                {
                    if (merchantNameField.Value.ValueType == FieldValueType.String)
                    {
                        string merchantName = merchantNameField.Value.AsString();

                        Console.WriteLine($"Merchant Name: '{merchantName}', with confidence {merchantNameField.Confidence}");
                    }
                }

                FormField transactionDateField;
                if (receipt.Fields.TryGetValue("TransactionDate", out transactionDateField))
                {
                    if (transactionDateField.Value.ValueType == FieldValueType.Date)
                    {
                        DateTime transactionDate = transactionDateField.Value.AsDate();

                        Console.WriteLine($"Transaction Date: '{transactionDate}', with confidence {transactionDateField.Confidence}");
                    }
                }

                FormField itemsField;
                if (receipt.Fields.TryGetValue("Items", out itemsField))
                {
                    if (itemsField.Value.ValueType == FieldValueType.List)
                    {
                        foreach (FormField itemField in itemsField.Value.AsList())
                        {
                            Console.WriteLine("Item:");

                            if (itemField.Value.ValueType == FieldValueType.Dictionary)
                            {
                                IReadOnlyDictionary<string, FormField> itemFields = itemField.Value.AsDictionary();

                                FormField itemNameField;
                                if (itemFields.TryGetValue("Name", out itemNameField))
                                {
                                    if (itemNameField.Value.ValueType == FieldValueType.String)
                                    {
                                        string itemName = itemNameField.Value.AsString();

                                        Console.WriteLine($"    Name: '{itemName}', with confidence {itemNameField.Confidence}");
                                    }
                                }

                                FormField itemTotalPriceField;
                                if (itemFields.TryGetValue("TotalPrice", out itemTotalPriceField))
                                {
                                    if (itemTotalPriceField.Value.ValueType == FieldValueType.Float)
                                    {
                                        float itemTotalPrice = itemTotalPriceField.Value.AsFloat();

                                        Console.WriteLine($"    Total Price: '{itemTotalPrice}', with confidence {itemTotalPriceField.Confidence}");
                                    }
                                }
                            }
                        }
                    }
                }
                FormField totalField;
                if (receipt.Fields.TryGetValue("Total", out totalField))
                {
                    if (totalField.Value.ValueType == FieldValueType.Float)
                    {
                        float total = totalField.Value.AsFloat();

                        Console.WriteLine($"Total: '{total}', with confidence '{totalField.Confidence}'");
                    }
                }
            }
        }

        private static async Task<string> TrainModel(FormTrainingClient trainingClient, string trainingDataUrl)
        {
            CustomFormModel model = await trainingClient.StartTrainingAsync(new Uri(trainingDataUrl), useTrainingLabels: false)
            .WaitForCompletionAsync();

            Console.WriteLine($"Custom Model Info:");
            Console.WriteLine($"    Model Id: {model.ModelId}");
            Console.WriteLine($"    Model Status: {model.Status}");
            Console.WriteLine($"    Training model started on: {model.TrainingStartedOn}");
            Console.WriteLine($"    Training model completed on: {model.TrainingCompletedOn}");

            foreach (CustomFormSubmodel submodel in model.Submodels)
            {
                Console.WriteLine($"Submodel Form Type: {submodel.FormType}");
                foreach (CustomFormModelField field in submodel.Fields.Values)
                {
                    Console.Write($"    FieldName: {field.Name}");
                    if (field.Label != null)
                    {
                        Console.Write($", FieldLabel: {field.Label}");
                    }
                    Console.WriteLine("");
                }
            }
            return model.ModelId;
        }

        // Analyze PDF form data
        private static async Task AnalyzePdfForm(FormRecognizerClient recognizerClient, int modelId, string formUrl)
        {
            RecognizedFormCollection forms = await recognizerClient
            .StartRecognizeCustomFormsFromUri(modelId.ToString(), new Uri(formUrl))
            .WaitForCompletionAsync();

            foreach (RecognizedForm form in forms)
            {
                Console.WriteLine($"Form of type: {form.FormType}");
                foreach (FormField field in form.Fields.Values)
                {
                    Console.WriteLine($"Field '{field.Name}: ");

                    if (field.LabelData != null)
                    {
                        Console.WriteLine($"    Label: '{field.LabelData.Text}");
                    }

                    Console.WriteLine($"    Value: '{field.ValueData.Text}");
                    Console.WriteLine($"    Confidence: '{field.Confidence}");
                }
                Console.WriteLine("Table data:");
                foreach (FormPage page in form.Pages)
                {
                    for (int i = 0; i < page.Tables.Count; i++)
                    {
                        FormTable table = page.Tables[i];
                        Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");
                        foreach (FormTableCell cell in table.Cells)
                        {
                            Console.WriteLine($"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains {(cell.IsHeader ? "header" : "text")}: '{cell.Text}'");
                        }
                    }
                }
            }
        }

        private static void ManageModels(FormTrainingClient trainingClient, string trainingFileUrl)
        {
            // Check number of models in the FormRecognizer account, 
            // and the maximum number of models that can be stored.
            AccountProperties accountProperties = trainingClient.GetAccountProperties();
            Console.WriteLine($"Account has {accountProperties.CustomModelCount} models.");
            Console.WriteLine($"It can have at most {accountProperties.CustomModelLimit} models.");

            Pageable<CustomFormModelInfo> models = trainingClient.GetCustomModels();

            foreach (CustomFormModelInfo modelInfo in models)
            {
                Console.WriteLine($"Custom Model Info:");
                Console.WriteLine($"    Model Id: {modelInfo.ModelId}");
                Console.WriteLine($"    Model Status: {modelInfo.Status}");
                Console.WriteLine($"    Training model started on: {modelInfo.TrainingStartedOn}");
                Console.WriteLine($"    Training model completed on: {modelInfo.TrainingCompletedOn}");
            }
        }
     }
}