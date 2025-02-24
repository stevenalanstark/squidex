﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Squidex.Domain.Apps.Entities.Assets.Repositories;
using Squidex.Infrastructure;

namespace Squidex.Domain.Apps.Entities.Assets.Queries
{
    public sealed class AssetQueryService : IAssetQueryService
    {
        private readonly IAssetEnricher assetEnricher;
        private readonly IAssetRepository assetRepository;
        private readonly AssetQueryParser queryParser;

        public AssetQueryService(
            IAssetEnricher assetEnricher,
            IAssetRepository assetRepository,
            AssetQueryParser queryParser)
        {
            Guard.NotNull(assetEnricher);
            Guard.NotNull(assetRepository);
            Guard.NotNull(queryParser);

            this.assetEnricher = assetEnricher;
            this.assetRepository = assetRepository;
            this.queryParser = queryParser;
        }

        public async Task<IEnrichedAssetEntity?> FindAssetAsync(Context context, Guid id)
        {
            var asset = await assetRepository.FindAssetAsync(id);

            if (asset != null)
            {
                return await assetEnricher.EnrichAsync(asset, context);
            }

            return null;
        }

        public async Task<IReadOnlyList<IEnrichedAssetEntity>> QueryByHashAsync(Context context, Guid appId, string hash)
        {
            Guard.NotNull(hash);

            var assets = await assetRepository.QueryByHashAsync(appId, hash);

            return await assetEnricher.EnrichAsync(assets, context);
        }

        public async Task<IResultList<IEnrichedAssetEntity>> QueryAsync(Context context, Q query)
        {
            Guard.NotNull(context);
            Guard.NotNull(query);

            IResultList<IAssetEntity> assets;

            if (query.Ids != null && query.Ids.Count > 0)
            {
                assets = await QueryByIdsAsync(context, query);
            }
            else
            {
                assets = await QueryByQueryAsync(context, query);
            }

            var enriched = await assetEnricher.EnrichAsync(assets, context);

            return ResultList.Create(assets.Total, enriched);
        }

        private async Task<IResultList<IAssetEntity>> QueryByQueryAsync(Context context, Q query)
        {
            var parsedQuery = queryParser.ParseQuery(context, query);

            return await assetRepository.QueryAsync(context.App.Id, parsedQuery);
        }

        private async Task<IResultList<IAssetEntity>> QueryByIdsAsync(Context context, Q query)
        {
            var assets = await assetRepository.QueryAsync(context.App.Id, new HashSet<Guid>(query.Ids));

            return Sort(assets, query.Ids);
        }

        private static IResultList<IAssetEntity> Sort(IResultList<IAssetEntity> assets, IReadOnlyList<Guid> ids)
        {
            return assets.SortSet(x => x.Id, ids);
        }
    }
}
