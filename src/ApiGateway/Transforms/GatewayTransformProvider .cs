using Yarp.ReverseProxy.Transforms.Builder;

namespace MicroMart.ApiGateway.Transforms;

public class GatewayTransformProvider : ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        context.RequestTransforms.Add(new GatewayRequestTransform());
    }

    public void Validate(TransformRouteValidationContext context)
    {
        // Optional validation logic
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
   
    }
}
