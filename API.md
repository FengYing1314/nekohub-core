# Blog Core API 文档（便于 LLM 理解）

本项目提供一个基于 ASP.NET Core 的博客后端核心能力，包含文章的增删改查、筛选、分页、统计、发布/取消发布等 API。数据暂存于内存中（模拟数据库）。

基础信息
- 基础路径：/api/posts
- 返回格式：application/json（System.Text.Json，camelCase 属性名）
- 时间字段：ISO 8601 UTC 时间（例如 2025-01-01T12:00:00Z）
- 错误格式：RFC 7807 Problem Details（Validation 使用 ValidationProblemDetails）

通用模型
1) Post（文章）
- id: number
- title: string（必填，1..200）
- content: string（必填，长度 >= 10）
- createdAt: string（UTC ISO 时间）
- updatedAt: string（UTC ISO 时间）
- isPublished: boolean

2) PagedResponse<T>
- items: T[]
- total: number（总条数）
- page: number（当前页，从 1 开始）
- pageSize: number（每页条数）
- totalPages: number（总页数）
- hasNext: boolean
- hasPrevious: boolean

3) PostPatchRequest（部分更新）
- title?: string（长度 1..200）
- content?: string（长度 >= 10）
- isPublished?: boolean

4) PostStats（统计）
- total: number（总文章数）
- published: number（已发布数量）
- drafts: number（草稿数量）

统一错误返回结构（节选）
- 400 Bad Request（ValidationProblemDetails）
{
  "type": "about:blank",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["文章标题不能为空或全是空白字符"],
    "Content": ["文章内容至少需要 10 个字符"]
  }
}

- 404 Not Found
{
  "status": 404,
  "title": "Not Found",
  "detail": "文章 ID 123 未找到"
}

- 201 Created（POST 成功）
Headers: Location: /api/posts/{id}
Body: 新创建的 Post

API 列表
1) 获取文章列表（可筛选/排序）
GET /api/posts
Query 参数：
- isPublished?: boolean（true 仅已发布，false 仅草稿）
- keyword?: string（标题或内容模糊匹配，不区分大小写）
- sortBy?: string（title|createdAt|updatedAt，默认 createdAt）
- sortOrder?: string（asc|desc，默认 desc）

响应：Post[]

2) 分页获取文章
GET /api/posts/paged
Query 参数：
- page?: number（默认 1，最小 1）
- pageSize?: number（默认 10，1..100）
- isPublished?: boolean
- keyword?: string
- sortBy?: string（title|createdAt|updatedAt）
- sortOrder?: string（asc|desc）

响应：PagedResponse<Post>

3) 获取单篇文章
GET /api/posts/{id}
响应：Post 或 404

4) 创建文章
POST /api/posts
Body: Post 的可写字段
{
  "title": "标题（1..200）",
  "content": "内容（>=10）",
  "isPublished": false
}

响应：201 Created + Location + Post
注意：服务端会自动分配 id、createdAt、updatedAt（UTC）

5) 更新文章（全部字段）
PUT /api/posts/{id}
Body: Post 的可写字段（可包含 id，但必须与路径参数一致，否则 400）
{
  "id": 1,
  "title": "更新后的标题",
  "content": "更新后的内容",
  "isPublished": true
}

响应：200 OK + Post

6) 部分更新文章
PATCH /api/posts/{id}
Body: PostPatchRequest（仅更新提供的字段）
{
  "title": "仅更新标题"
}

响应：200 OK + Post

7) 删除文章
DELETE /api/posts/{id}
响应：200 OK（字符串消息）或 404

8) 发布文章
POST /api/posts/{id}/publish
响应：200 OK + Post（isPublished=true，updatedAt 更新）

9) 取消发布文章
POST /api/posts/{id}/unpublish
响应：200 OK + Post（isPublished=false，updatedAt 更新）

10) 获取统计
GET /api/posts/stats
响应：PostStats

筛选/排序说明
- keyword 按 Title、Content 做不区分大小写包含匹配。
- sortBy 支持：title、createdAt、updatedAt（大小写不敏感）。
- sortOrder：asc/desc（大小写不敏感），默认 desc。

示例请求（curl）
- 获取已发布文章，按更新时间降序：
  curl "http://localhost:5249/api/posts?isPublished=true&sortBy=updatedAt&sortOrder=desc"

- 分页 + 关键词搜索：
  curl "http://localhost:5249/api/posts/paged?page=1&pageSize=10&keyword=博客"

- 创建：
  curl -X POST http://localhost:5249/api/posts \
       -H "Content-Type: application/json" \
       -d '{"title":"新文章","content":"这里是内容至少十个字","isPublished":false}'

- 全量更新：
  curl -X PUT http://localhost:5249/api/posts/1 \
       -H "Content-Type: application/json" \
       -d '{"id":1,"title":"更新后标题","content":"更新后内容不少于十个字","isPublished":true}'

- 部分更新：
  curl -X PATCH http://localhost:5249/api/posts/1 \
       -H "Content-Type: application/json" \
       -d '{"title":"仅更新标题"}'

- 发布 / 取消发布：
  curl -X POST http://localhost:5249/api/posts/1/publish
  curl -X POST http://localhost:5249/api/posts/1/unpublish

- 删除：
  curl -X DELETE http://localhost:5249/api/posts/1

MAUI 管理端对接建议
- 使用 HttpClient 调用上述 API，建议统一封装 ApiClient 层：
  - GET 列表/分页用于表格展示，支持关键字搜索、筛选发布状态、排序、分页。
  - 详情页用于查看/编辑，提交 PUT/PATCH。
  - 提供发布/取消发布按钮调用对应 API。
  - 删除操作前做二次确认。
- 将 400 返回的 ValidationProblemDetails 中的 errors 映射到表单校验提示。
- 时间字段使用 UTC，前端显示时按本地时区格式化。
- 由于当前后端为内存存储，应用重启后数据会重置；生产请替换为持久化存储（数据库）。

兼容性与注意事项
- JSON 属性名为 camelCase（e.g. createdAt、updatedAt、isPublished）。
- 服务器对传入的 title、content 做 Trim 后再校验，防止纯空白。
- PUT 需要路径 id 与 Body 内 id 一致（如 Body 提供 id），否则返回 400。
- 线程安全：控制器内部使用锁保护内存数据（模拟并发安全）。
- 示例 .http 文件：nekohub-core.http，便于在 IDE 中直接测试。
