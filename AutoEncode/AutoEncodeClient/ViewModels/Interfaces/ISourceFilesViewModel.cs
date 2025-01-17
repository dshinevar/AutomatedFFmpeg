﻿using AutoEncodeClient.Collections;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;

namespace AutoEncodeClient.ViewModels.Interfaces;

public interface ISourceFilesViewModel : IViewModel
{
    /// <summary>Requests initial source file load. </summary>
    void Initialize();

    void Shutdown();

    ObservableDictionary<string, ISourceFilesDirectoryViewModel> SourceFiles { get; }
}
