﻿/*
 * Copyright (c) 2017 Håkan Edling
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 * 
 * http://github.com/piranhacms/piranha
 * 
 */

using Microsoft.EntityFrameworkCore;
using Piranha.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Piranha.Repositories
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        private readonly Api api;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="api">The current api</param>
        /// <param name="db">The current db context</param>
        /// <param name="cache">The optional model cache</param>
        public CategoryRepository(Api api, IDb db, ICache cache = null) 
            : base(db, cache) 
        { 
            this.api = api;
        }

        /// <summary>
        /// Gets all available models for the specified blog.
        /// </summary>
        /// <param name="id">The blog id</param>
        /// <returns>The available models</returns>
        public IEnumerable<Category> GetAll(Guid blogId) {
            var models = new List<Category>();
            var categories = db.Categories
                .AsNoTracking()
                .Where(c => c.BlogId == blogId)
                .Select(c => c.Id);

            foreach (var c in categories) {
                var model = GetById(c);
                if (model != null)
                    models.Add(model);
            }
            return models;
        }

        /// <summary>
        /// Gets the model with the given slug.
        /// </summary>
        /// <param name="blogId">The blog id</param>
        /// <param name="slug">The unique slug</param>
        /// <returns>The model</returns>
        public Category GetBySlug(Guid blogId, string slug) {
            var id = cache != null ? cache.Get<Guid?>($"Category_{blogId}_{slug}") : null;
            Category model = null;

            if (id.HasValue) {
                model = GetById(id.Value);
            } else {
                model = db.Categories
                    .AsNoTracking()
                    .FirstOrDefault(c => c.BlogId == blogId && c.Slug == slug);

                if (cache != null && model != null)
                    AddToCache(model);
            }
            return model;
        }

        /// <summary>
        /// Gets the model with the given title
        /// </summary>
        /// <param name="blogId">The blog id</param>
        /// <param name="title">The unique title</param>
        /// <returns>The model</returns>
        public Category GetByTitle(Guid blogId, string title) {
            var model = db.Categories
                .AsNoTracking()
                .SingleOrDefault(c => c.BlogId == blogId && c.Title == title);

            if (cache != null && model != null)
                AddToCache(model);
            return model;
        }
        
        #region Protected methods
        /// <summary>
        /// Adds a new model to the database.
        /// </summary>
        /// <param name="model">The model</param>
        protected override void Add(Category model) {
            PrepareInsert(model);

            // Check required
            if (string.IsNullOrWhiteSpace(model.Title))
                throw new ArgumentException("Title is required for Category");

            // Ensure slug
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = Utils.GenerateSlug(model.Title);
            else model.Slug = Utils.GenerateSlug(model.Slug);

            db.Categories.Add(model);
        }

        /// <summary>
        /// Updates the given model in the database.
        /// </summary>
        /// <param name="model">The model</param>
        protected override void Update(Category model) {
            PrepareUpdate(model);

            // Check required
            if (string.IsNullOrWhiteSpace(model.Title))
                throw new ArgumentException("Title is required for Category");

            // Ensure slug
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = Utils.GenerateSlug(model.Title);
            else model.Slug = Utils.GenerateSlug(model.Slug);

            var category = db.Categories.FirstOrDefault(c => c.Id == model.Id);
            if (category != null) {
                App.Mapper.Map<Category, Category>(model, category);
            }
        }

        /// <summary>
        /// Adds the given model to cache.
        /// </summary>
        /// <param name="model">The model</param>
        protected override void AddToCache(Category model) {
            cache.Set(model.Id.ToString(), model);
            cache.Set($"Category_{model.BlogId}_{model.Slug}", model.Id);
        }

        /// <summary>
        /// Removes the given model from cache.
        /// </summary>
        /// <param name="model">The model</param>
        protected override void RemoveFromCache(Category model) {
            cache.Remove(model.Id.ToString());
            cache.Remove($"Category_{model.BlogId}_{model.Slug}");
        }        
        #endregion
    }
}
