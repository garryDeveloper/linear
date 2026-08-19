using FastEndpoints;

using FluentValidation;

using Linear.Domain.Comments;

namespace Linear.Web.Features.Comments.Update;

public sealed class UpdateCommentValidator : Validator<UpdateCommentRequest>
{
    public UpdateCommentValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("El comentario no puede estar vacío.")
            .MaximumLength(Comment.MaxContentLength);
    }
}
