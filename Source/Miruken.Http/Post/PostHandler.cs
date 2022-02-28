namespace Miruken.Http.Post;

using System.Net.Http;
using System.Threading.Tasks;
using Callback;

public class PostHandler : ResourceHandler
{
    [Handles]
    public async Task<PostResponse<TResource>>
        Post<TPost, TResource>(PostRequest<TPost, TResource> post,
            HttpService http, IHandler composer)
    {
        var request = GetRequest(post.Request, HttpMethod.Post)
                      ?? new HttpRequestMessage(HttpMethod.Post, post.ResourceUri)
                      {
                          Content = GetContent(post.Request, post)
                      };
        var response = await http.SendRequest(post, request, composer,
            out var options).ConfigureAwait(false);
        var resource = await ExtractResource<TResource>(response, options);
        var postResponse = new PostResponse<TResource>(resource);
        SetResponseUri(postResponse, response);
        return postResponse;
    }
}