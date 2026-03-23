using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Embedding.Models.Voyage
{
    /// <summary>
    /// Known multimodal embedding models from Voyage.
    /// </summary>
    public class EmbeddingModelVoyageMultimodal : BaseVendorModelProvider
    {
        /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
        public override LLmProviders Provider => LLmProviders.Voyage;

        /// <summary>
        /// Voyage Multimodal Gen 3 models.
        /// </summary>
        public readonly EmbeddingModelVoyageMultimodalGen3 Gen3 = new EmbeddingModelVoyageMultimodalGen3();

        /// <summary>
        /// Voyage Multimodal Gen 3.5 models.
        /// </summary>
        public readonly EmbeddingModelVoyageMultimodalGen35 Gen35 = new EmbeddingModelVoyageMultimodalGen35();

        /// <summary>
        /// All owned models.
        /// </summary>
        public override List<IModel> AllModels => ModelsAll;

        /// <summary>
        /// Checks whether the model is owned by the provider.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override bool OwnsModel(string model)
        {
            return AllModelsMap.Contains(model);
        }

        /// <summary>
        /// Map of models owned by the provider.
        /// </summary>
        public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;

        private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
        {
            HashSet<string> map = new HashSet<string>();
            ModelsAll.ForEach(x => { map.Add(x.Name); });
            return map;
        });

        /// <summary>
        /// <inheritdoc cref="AllModels"/>
        /// </summary>
        public static List<IModel> ModelsAll => LazyModelsAll.Value;

        private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() =>
        {
            List<IModel> list = new List<IModel>();
            list.AddRange(EmbeddingModelVoyageMultimodalGen3.ModelsAll);
            list.AddRange(EmbeddingModelVoyageMultimodalGen35.ModelsAll);
            return list;
        });

        internal EmbeddingModelVoyageMultimodal()
        {

        }
    }
}
