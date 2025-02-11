using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Michsky.UI.Heat;
using SFB;
using TMPro;

// Классы для сериализации данных замен
[System.Serializable]
public class ReplacementRuleData {
    public string search;
    public string replacement1;
    public string replacement2;
    public string replacement3;
}

[System.Serializable]
public class ReplacementRulesData {
    public List<ReplacementRuleData> rules = new List<ReplacementRuleData>();
}

public class CSVProcessor : MonoBehaviour
{
    [Header("UI Элементы")]
    public RectTransform pairsContainer;

    public GameObject pairRowPrefab;
    public PanelButton addRowButton;
    public PanelButton selectFolderButton;
    public PanelButton processButton;
    public TMP_Text folderPathText;

    [Header("Логирование")]
    public Transform logContentContainer;
    public GameObject logEntryPrefab;
    public ScrollRect logScrollRect;

    [Header("Выход из приложения")]
    public ButtonManager exitButton;

    private string selectedFolder = "";
    private List<ReplacementRow> replacementRows = new List<ReplacementRow>();

    // Ключи для сохранения данных в PlayerPrefs
    private const string PREFS_KEY_REPLACEMENTS = "CSVProcessor_Replacements";
    private const string PREFS_KEY_FOLDERPATH = "CSVProcessor_FolderPath";

    void Start()
    {
        addRowButton.onClick.AddListener(AddReplacementRow);
        selectFolderButton.onClick.AddListener(SelectFolder);
        processButton.onClick.AddListener(ProcessCSVFiles);
        exitButton.onClick.AddListener(ExitApplication);
        
        LoadData();

        LogMessage("Приложение запущено. Данные загружены.");
    }
    
    void OnApplicationQuit()
    {
        SaveData();
    }
    
    void LogMessage(string message)
    {
        Debug.Log(message);
        if (logContentContainer != null && logEntryPrefab != null)
        {
            GameObject newLogEntry = Instantiate(logEntryPrefab, logContentContainer);
            TMP_Text logTextComponent = newLogEntry.GetComponent<TMP_Text>();
            if (logTextComponent != null)
            {
                logTextComponent.text = message;
            }

            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
    
    public void AddReplacementRow()
    {
        GameObject newRowObj = Instantiate(pairRowPrefab, pairsContainer);
        ReplacementRow row = newRowObj.GetComponent<ReplacementRow>();
        if (row != null)
        {
            row.SetupDeleteButton();
            row.OnDelete = RemoveReplacementRow;
            replacementRows.Add(row);
            LogMessage("Добавлена новая строка замены.");
        }
    }
    
    public void RemoveReplacementRow(ReplacementRow row)
    {
        if (replacementRows.Contains(row))
        {
            replacementRows.Remove(row);
            Destroy(row.gameObject);
            LogMessage("Строка замены удалена.");
        }
    }

    // Выбор папки с помощью Standalone File Browser
    void SelectFolder()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Выберите папку с CSV-файлами", "", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            selectedFolder = paths[0];
            folderPathText.text = selectedFolder;
            LogMessage("Выбрана папка: " + selectedFolder);
        }
        else
        {
            LogMessage("Папка не выбрана.");
        }
    }

    // Обработка всех CSV-файлов в выбранной папке
    void ProcessCSVFiles()
    {
        if (string.IsNullOrEmpty(selectedFolder))
        {
            LogMessage("Ошибка: Папка не выбрана!");
            return;
        }

        LogMessage("Начинается обработка CSV-файлов в папке: " + selectedFolder);

        // Получаем все файлы с расширением .csv
        string[] csvFiles = Directory.GetFiles(selectedFolder, "*.csv");
        if (csvFiles.Length == 0)
        {
            LogMessage("В выбранной папке не найдено CSV-файлов.");
            return;
        }

        // Создаём папку "Processed" внутри выбранной папки для сохранения результатов
        string outputFolder = Path.Combine(selectedFolder, "Processed");
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            LogMessage("Создана папка для обработанных файлов: " + outputFolder);
        }

        // Обрабатываем каждый CSV-файл
        foreach (string filePath in csvFiles)
        {
            LogMessage("Обработка файла: " + Path.GetFileName(filePath));
            ProcessFile(filePath, outputFolder);
            LogMessage("Файл обработан: " + Path.GetFileName(filePath));
        }

        LogMessage("Обработка всех CSV-файлов завершена!");
    }

    // Обработка одного CSV-файла
    void ProcessFile(string inputFilePath, string outputFolder)
    {
        // Считываем все строки файла (UTF-8)
        string[] lines = File.ReadAllLines(inputFilePath, System.Text.Encoding.UTF8);
        List<string> newLines = new List<string>();

        foreach (string line in lines)
        {
            // Разбиваем строку по запятой
            string[] columns = line.Split(',');
            if (columns.Length < 2)
            {
                newLines.Add(line);
                continue;
            }

            // Обрабатываем вторую колонку (текст после первой запятой)
            string originalText = columns[1];
            string newText = originalText;

            // Для каждой строки замены ищем в тексте поисковое слово и, если оно найдено,
            // выбираем случайный вариант замены из непустых
            foreach (var row in replacementRows)
            {
                string searchTerm = row.searchInput.text;
                if (!string.IsNullOrEmpty(searchTerm) && newText.Contains(searchTerm))
                {
                    List<string> candidates = new List<string>();
                    if (!string.IsNullOrEmpty(row.replacementInput1.text))
                        candidates.Add(row.replacementInput1.text);
                    if (!string.IsNullOrEmpty(row.replacementInput2.text))
                        candidates.Add(row.replacementInput2.text);
                    if (!string.IsNullOrEmpty(row.replacementInput3.text))
                        candidates.Add(row.replacementInput3.text);

                    if (candidates.Count > 0)
                    {
                        int randomIndex = Random.Range(0, candidates.Count);
                        string chosenReplacement = candidates[randomIndex];
                        newText = newText.Replace(searchTerm, chosenReplacement);
                    }
                }
            }
            columns[1] = newText;
            string newLine = string.Join(",", columns);
            newLines.Add(newLine);
        }

        // Сохраняем обработанный файл в выходной папке с тем же именем
        string fileName = Path.GetFileName(inputFilePath);
        string outputFilePath = Path.Combine(outputFolder, fileName);
        File.WriteAllLines(outputFilePath, newLines.ToArray(), System.Text.Encoding.UTF8);
    }

    // Сохранение данных (списка замен и выбранного пути) в PlayerPrefs
    void SaveData()
    {
        ReplacementRulesData data = new ReplacementRulesData();
        foreach (var row in replacementRows)
        {
            ReplacementRuleData rule = new ReplacementRuleData();
            rule.search = row.searchInput.text;
            rule.replacement1 = row.replacementInput1.text;
            rule.replacement2 = row.replacementInput2.text;
            rule.replacement3 = row.replacementInput3.text;
            data.rules.Add(rule);
        }
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PREFS_KEY_REPLACEMENTS, json);
        PlayerPrefs.SetString(PREFS_KEY_FOLDERPATH, selectedFolder);
        PlayerPrefs.Save();
        LogMessage("Данные сохранены.");
    }

    // Загрузка данных из PlayerPrefs и создание соответствующих префабов
    void LoadData()
    {
        if (PlayerPrefs.HasKey(PREFS_KEY_REPLACEMENTS))
        {
            string json = PlayerPrefs.GetString(PREFS_KEY_REPLACEMENTS);
            ReplacementRulesData data = JsonUtility.FromJson<ReplacementRulesData>(json);
            if (data != null && data.rules != null)
            {
                foreach (var rule in data.rules)
                {
                    GameObject newRowObj = Instantiate(pairRowPrefab, pairsContainer);
                    ReplacementRow row = newRowObj.GetComponent<ReplacementRow>();
                    if (row != null)
                    {
                        row.searchInput.text = rule.search;
                        row.replacementInput1.text = rule.replacement1;
                        row.replacementInput2.text = rule.replacement2;
                        row.replacementInput3.text = rule.replacement3;
                        row.SetupDeleteButton();
                        row.OnDelete = RemoveReplacementRow;
                        replacementRows.Add(row);
                    }
                }
                LogMessage("Загружено " + data.rules.Count + " правил замены.");
            }
        }
        if (PlayerPrefs.HasKey(PREFS_KEY_FOLDERPATH))
        {
            selectedFolder = PlayerPrefs.GetString(PREFS_KEY_FOLDERPATH);
            folderPathText.text = selectedFolder;
            LogMessage("Загружен путь к папке: " + selectedFolder);
        }
    }

    // Метод для полного завершения работы приложения
    void ExitApplication()
    {
        LogMessage("Выход из приложения...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
